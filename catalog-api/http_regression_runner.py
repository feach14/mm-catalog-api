#!/usr/bin/env python3
import copy
import json
import os
import re
import socket
import ssl
import subprocess
import sys
import tempfile
import time
import urllib.error
import urllib.parse
import urllib.request
import uuid

ROOT = os.path.dirname(os.path.abspath(__file__))
API = os.environ.get("CATALOG_API_URL", "https://localhost:5005")
ACCOUNTS = os.environ.get("ACCOUNTS_API_URL", "https://localhost:5003")
CTX = ssl._create_unverified_context()
cookie = None
server = None
server_log = None
history_run_id = None
results = {}
created_materials = []
created_categories = []
created_sheet_sizes = []
created_manufacturers = []
primary_sheet_size_id = None
primary_manufacturer_id = None
guids = []


def load_test_credentials():
    default_path = os.path.abspath(os.path.join(
        ROOT, "..", "..", "mm-accounts-api", "accounts-api", "docs", "TEST_CREDENTIALS.md"))
    path = os.environ.get("ACCOUNTS_TEST_CREDENTIALS_FILE", default_path)
    try:
        with open(path, encoding="utf-8") as credentials_file:
            contents = credentials_file.read()
    except OSError as error:
        raise RuntimeError(f"Cannot read Accounts API test credentials at {path}: {error}") from error

    password_match = re.search(r"^- Пароль: `([^`]+)`$", contents, re.MULTILINE)
    if password_match is None:
        raise RuntimeError(f"Test password is missing in {path}")

    role_labels = {
        "user": "Обычный пользователь",
        "manager": "Менеджер",
        "administrator": "Администратор",
        "tester": "Тестер",
    }
    credentials = {"password": password_match.group(1)}
    for role, label in role_labels.items():
        match = re.search(rf"^\|\s*{re.escape(label)}\s*\|\s*`([^`]+)`", contents, re.MULTILINE)
        if match is None:
            raise RuntimeError(f"Credentials for role {role} are missing in {path}")
        credentials[role] = match.group(1)
    return credentials


def request(method, path, data=None, auth=False, auth_cookie=None, headers=None, raw=False, base=API):
    hdrs = dict(headers or {})
    if auth_cookie:
        hdrs["Cookie"] = auth_cookie
    elif auth and cookie:
        hdrs["Cookie"] = cookie
    body = data
    if data is not None and not raw:
        body = json.dumps(data).encode()
        hdrs["Content-Type"] = "application/json"
    req = urllib.request.Request(base + path, data=body, headers=hdrs, method=method)
    try:
        with urllib.request.urlopen(req, context=CTX, timeout=30) as response:
            return response.status, dict(response.headers), response.read()
    except urllib.error.HTTPError as error:
        return error.code, dict(error.headers), error.read()


def json_body(response):
    return json.loads(response[2].decode()) if response[2] else None


def expect(status, expected, label):
    if status != expected:
        raise AssertionError(f"{label}: expected HTTP {expected}, got {status}")


def mark(case, detail):
    results[case] = detail


def multipart(name, filename, content, content_type):
    boundary = "----CodexCatalog" + uuid.uuid4().hex
    payload = (
        f"--{boundary}\r\n"
        f'Content-Disposition: form-data; name="{name}"; filename="{filename}"\r\n'
        f"Content-Type: {content_type}\r\n\r\n"
    ).encode() + content + f"\r\n--{boundary}--\r\n".encode()
    return payload, {"Content-Type": f"multipart/form-data; boundary={boundary}"}


def upload(filename, content, content_type, auth=True):
    payload, headers = multipart("file", filename, content, content_type)
    return request("POST", "/api/materials/images", payload, auth=auth, headers=headers, raw=True)


def start_server():
    global server, server_log
    api_url = urllib.parse.urlparse(API)
    try:
        with socket.create_connection((api_url.hostname, api_url.port), timeout=0.5):
            raise RuntimeError(
                f"Catalog API port {api_url.port} is already occupied; "
                "stop the existing process or set CATALOG_API_URL to a free port"
            )
    except (ConnectionRefusedError, TimeoutError, OSError):
        pass

    server_log = tempfile.NamedTemporaryFile(prefix="catalog-http-", suffix=".log", dir=ROOT, delete=False)
    env = dict(os.environ)
    env["ASPNETCORE_ENVIRONMENT"] = "Development"
    env["ASPNETCORE_URLS"] = API
    dll = os.path.join(ROOT, "Catalog.Api", "bin", "Debug", "net10.0", "Catalog.Api.dll")
    server = subprocess.Popen(["dotnet", dll], cwd=os.path.join(ROOT, "Catalog.Api"), env=env,
                              stdout=server_log, stderr=subprocess.STDOUT)
    for _ in range(60):
        if server.poll() is not None:
            raise RuntimeError("Catalog API terminated during startup")
        try:
            status, _, _ = request("GET", "/api/materials/categories")
            if status == 200:
                return
        except Exception:
            pass
        time.sleep(0.25)
    raise RuntimeError("Catalog API did not start")


def stop_server():
    global server, server_log
    if server and server.poll() is None:
        server.terminate()
        try:
            server.wait(timeout=10)
        except subprocess.TimeoutExpired:
            server.kill()
            server.wait(timeout=5)
    server = None
    if server_log:
        name = server_log.name
        server_log.close()
        try:
            os.remove(name)
        except FileNotFoundError:
            pass
        server_log = None


def restart_server():
    stop_server()
    start_server()


def signin(phone, password, role):
    response = request("POST", "/api/sign/in", {
        "phone": phone, "password": password, "isManager": False
    }, base=ACCOUNTS)
    expect(response[0], 200, f"{role} sign-in")
    raw_cookie = response[1].get("Set-Cookie")
    if not raw_cookie or "AccountsApiCookie=" not in raw_cookie:
        raise AssertionError(f"AccountsApiCookie was not returned for {role}")
    return raw_cookie.split(";", 1)[0]


def categories():
    response = request("GET", "/api/materials/categories")
    expect(response[0], 200, "categories list")
    return json_body(response)["items"]


def sheet_sizes():
    response = request("GET", "/api/materials/sheet-sizes")
    expect(response[0], 200, "material sheet sizes list")
    return json_body(response)["items"]


def manufacturers():
    response = request("GET", "/api/materials/manufacturers")
    expect(response[0], 200, "manufacturers list")
    return json_body(response)["items"]


def history(**filters):
    parameters = {"page": filters.pop("page", 1), "pageSize": filters.pop("page_size", 200)}
    parameters.update({key: value for key, value in filters.items() if value is not None})
    response = request("GET", "/api/catalog/history?" + urllib.parse.urlencode(parameters), auth=True)
    expect(response[0], 200, "catalog history")
    return json_body(response)


def relation_counts(path, relation_id):
    list_response = request("GET", path)
    expect(list_response[0], 200, f"{path} list for counters")
    list_item = next(item for item in json_body(list_response)["items"] if item["id"] == relation_id)
    card_response = request("GET", f"{path}/{relation_id}")
    expect(card_response[0], 200, f"{path} card for counters")
    card = json_body(card_response)
    list_counts = (list_item["materialsAnyCount"], list_item["materialsNotAnyCount"])
    card_counts = (card["materialsAnyCount"], card["materialsNotAnyCount"])
    if list_counts != card_counts:
        raise AssertionError(f"counter mismatch between list and card for {path}/{relation_id}")
    return list_counts


def admin_materials():
    response = request("GET", "/api/materials/admin", auth=True)
    expect(response[0], 200, "admin material list")
    return json_body(response)["materials"]


def public_materials(*calculators):
    query = "" if not calculators else "?" + urllib.parse.urlencode({"calculator": calculators}, doseq=True)
    response = request("GET", f"/api/materials{query}")
    expect(response[0], 200, f"public list {calculators or 'all'}")
    return json_body(response)["materials"]


def create_category(name):
    response = request("POST", "/api/materials/categories", {"name": name, "externalLink": "https://example.test/" + name}, auth=True)
    expect(response[0], 200, "create category")
    category_id = json_body(response)["id"]
    created_categories.append(category_id)
    return category_id


def create_sheet_size(name, height, width):
    response = request("POST", "/api/materials/sheet-sizes", {
        "name": name, "height": height, "width": width
    }, auth=True)
    expect(response[0], 200, "create material sheet size")
    size_id = json_body(response)["id"]
    created_sheet_sizes.append(size_id)
    return size_id


def create_manufacturer(name):
    response = request("POST", "/api/materials/manufacturers", {"name": name}, auth=True)
    expect(response[0], 200, "create manufacturer")
    manufacturer_id = json_body(response)["id"]
    created_manufacturers.append(manufacturer_id)
    return manufacturer_id


def material_model(name, category_id, flag, image=None):
    return {
        "categoryId": category_id,
        "sheetSizeId": primary_sheet_size_id,
        "manufacturerId": primary_manufacturer_id,
        "name": name,
        "article": "ART-" + name[-8:],
        "image": image,
        "count": 2,
        "depth": 18,
        "kvM": 5.796,
        "perimetrM": 9.74,
        "applicableToRaskroys": flag == "raskroy",
        "applicableToPvhFacades": flag == "pvhFacades",
        "applicableToEmalFacades": flag == "emalFacades",
        "commentOnMaterialIsRequired": False,
        "allowSecondItemInOrder": True,
        "externalLink": "https://example.test/material",
        "price": 123.45,
        "countTypeEnum": "M2"
    }


def create_material(model):
    response = request("POST", "/api/materials", model, auth=True)
    expect(response[0], 200, "create material")
    material_id = json_body(response)["id"]
    created_materials.append(material_id)
    return material_id


def delete_material(material_id):
    response = request("DELETE", f"/api/materials/{material_id}", auth=True)
    if response[0] == 200 and material_id in created_materials:
        created_materials.remove(material_id)
    return response


def cleanup():
    for material_id in list(reversed(created_materials)):
        try:
            delete_material(material_id)
        except Exception:
            pass
    for category_id in list(reversed(created_categories)):
        try:
            response = request("DELETE", f"/api/materials/categories/{category_id}", auth=True)
            if response[0] == 200:
                created_categories.remove(category_id)
        except Exception:
            pass
    for size_id in list(reversed(created_sheet_sizes)):
        try:
            response = request("DELETE", f"/api/materials/sheet-sizes/{size_id}", auth=True)
            if response[0] == 200:
                created_sheet_sizes.remove(size_id)
        except Exception:
            pass
    for manufacturer_id in list(reversed(created_manufacturers)):
        try:
            response = request("DELETE", f"/api/materials/manufacturers/{manufacturer_id}", auth=True)
            if response[0] == 200:
                created_manufacturers.remove(manufacturer_id)
        except Exception:
            pass
    if history_run_id:
        try:
            request("DELETE", f"/api/catalog/history/test-runs/{history_run_id}", auth=True)
        except Exception:
            pass


def run_history_scenarios(history_prefix, dimension_base, manager_cookie, tester_cookie, manager_phone):
    category_name = history_prefix + "-category"
    category_model = {"name": category_name, "externalLink": "https://example.test/history-category"}
    response = request("POST", "/api/materials/categories", category_model, auth_cookie=manager_cookie)
    expect(response[0], 200, "history category create")
    category_id = json_body(response)["id"]
    created_categories.append(category_id)

    size_name = history_prefix + "-size"
    size_model = {"name": size_name, "height": dimension_base + 100, "width": dimension_base + 101}
    response = request("POST", "/api/materials/sheet-sizes", size_model, auth_cookie=manager_cookie)
    expect(response[0], 200, "history sheet size create")
    size_id = json_body(response)["id"]
    created_sheet_sizes.append(size_id)

    manufacturer_name = history_prefix + "-manufacturer"
    response = request("POST", "/api/materials/manufacturers", {
        "name": manufacturer_name
    }, auth_cookie=manager_cookie)
    expect(response[0], 200, "history manufacturer create")
    manufacturer_id = json_body(response)["id"]
    created_manufacturers.append(manufacturer_id)

    material_name = history_prefix + "-material"
    material = material_model(material_name, category_id, "raskroy")
    material.update({"sheetSizeId": size_id, "manufacturerId": manufacturer_id})
    response = request("POST", "/api/materials", material, auth_cookie=manager_cookie)
    expect(response[0], 200, "history material create")
    material_id = json_body(response)["id"]
    created_materials.append(material_id)

    created_history = history(search=history_prefix)
    if created_history["totalCount"] != 4:
        raise AssertionError("history create events are not isolated")

    updated_category_name = category_name + "-updated"
    updated_category_model = dict(category_model, name=updated_category_name)
    expect(request("PUT", f"/api/materials/categories/{category_id}", updated_category_model,
                   auth_cookie=manager_cookie)[0], 200, "history category update")

    updated_size_name = size_name + "-updated"
    updated_size_model = dict(size_model, name=updated_size_name)
    expect(request("PUT", f"/api/materials/sheet-sizes/{size_id}", updated_size_model,
                   auth_cookie=manager_cookie)[0], 200, "history sheet size update")

    updated_manufacturer_name = manufacturer_name + "-updated"
    updated_manufacturer_model = {"name": updated_manufacturer_name}
    expect(request("PUT", f"/api/materials/manufacturers/{manufacturer_id}", updated_manufacturer_model,
                   auth_cookie=manager_cookie)[0], 200, "history manufacturer update")

    updated_material_name = material_name + "-updated"
    updated_material = copy.deepcopy(material)
    updated_material["name"] = updated_material_name
    expect(request("PUT", f"/api/materials/{material_id}", updated_material,
                   auth_cookie=manager_cookie)[0], 200, "history material update")

    updated_history = history(search=history_prefix)
    if updated_history["totalCount"] != 8:
        raise AssertionError("history update events are not isolated")
    update_items = [item for item in updated_history["items"] if item["actionType"] == "Update"]
    expected_name_changes = {
        "Category": (category_name, updated_category_name),
        "SheetSize": (size_name, updated_size_name),
        "Manufacturer": (manufacturer_name, updated_manufacturer_name),
        "Material": (material_name, updated_material_name),
    }
    if len(update_items) != 4:
        raise AssertionError("history update event count is incorrect")
    for item in update_items:
        old_name, new_name = expected_name_changes[item["entityType"]]
        if "назван" not in item["message"] or old_name not in item["message"] or new_name not in item["message"]:
            raise AssertionError("history update does not contain the expected old and new value")
        if item["message"].count("→") != 1:
            raise AssertionError("history update contains fields that were not changed")

    expect(request("PUT", f"/api/materials/categories/{category_id}", updated_category_model,
                   auth_cookie=manager_cookie)[0], 200, "history category no-op")
    expect(request("PUT", f"/api/materials/sheet-sizes/{size_id}", updated_size_model,
                   auth_cookie=manager_cookie)[0], 200, "history sheet size no-op")
    expect(request("PUT", f"/api/materials/manufacturers/{manufacturer_id}", updated_manufacturer_model,
                   auth_cookie=manager_cookie)[0], 200, "history manufacturer no-op")
    expect(request("PUT", f"/api/materials/{material_id}", updated_material,
                   auth_cookie=manager_cookie)[0], 200, "history material no-op")
    if history(search=history_prefix)["totalCount"] != 8:
        raise AssertionError("no-op update created a history event")

    helper_name = history_prefix + "-sort-helper"
    response = request("POST", "/api/materials/categories", {
        "name": helper_name, "externalLink": "https://example.test/history-sort-helper"
    }, auth_cookie=tester_cookie)
    expect(response[0], 200, "history sorting helper create")
    helper_id = json_body(response)["id"]
    created_categories.append(helper_id)
    expect(request("POST", "/api/materials/categories/change-order-col", {
        "categoryId": helper_id, "direction": "UP"
    }, auth_cookie=manager_cookie)[0], 200, "history sorting up")
    expect(request("POST", "/api/materials/categories/change-order-col", {
        "categoryId": helper_id, "direction": "DOWN"
    }, auth_cookie=manager_cookie)[0], 200, "history sorting down")
    expect(request("DELETE", f"/api/materials/categories/{helper_id}",
                   auth_cookie=tester_cookie)[0], 200, "history sorting helper cleanup")
    created_categories.remove(helper_id)
    if history(search=history_prefix)["totalCount"] != 8:
        raise AssertionError("sorting or tester cleanup created a history event")

    expect(request("POST", "/api/materials/categories", {
        "name": updated_category_name, "externalLink": "https://example.test/duplicate"
    }, auth_cookie=manager_cookie)[0], 400, "history unsuccessful command")
    if history(search=history_prefix)["totalCount"] != 8:
        raise AssertionError("unsuccessful command created a history event")
    if len([item for item in categories() if item["name"] == updated_category_name]) != 1:
        raise AssertionError("unsuccessful command partially changed catalog data")

    expect(request("DELETE", f"/api/materials/{material_id}", auth_cookie=manager_cookie)[0],
           200, "history material delete")
    created_materials.remove(material_id)
    expect(request("DELETE", f"/api/materials/categories/{category_id}", auth_cookie=manager_cookie)[0],
           200, "history category delete")
    created_categories.remove(category_id)
    expect(request("DELETE", f"/api/materials/sheet-sizes/{size_id}", auth_cookie=manager_cookie)[0],
           200, "history sheet size delete")
    created_sheet_sizes.remove(size_id)
    expect(request("DELETE", f"/api/materials/manufacturers/{manufacturer_id}", auth_cookie=manager_cookie)[0],
           200, "history manufacturer delete")
    created_manufacturers.remove(manufacturer_id)

    prefix_history = history(search=history_prefix)
    if prefix_history["totalCount"] != 12:
        raise AssertionError(f"unexpected isolated history event count: {prefix_history['totalCount']} instead of 12")
    items = prefix_history["items"]
    if any(item["userPhone"] != manager_phone for item in items):
        raise AssertionError("history contains an incorrect initiator phone")
    action_counts = {action: sum(item["actionType"] == action for item in items)
                     for action in ("Create", "Update", "Delete")}
    if action_counts != {"Create": 4, "Update": 4, "Delete": 4}:
        raise AssertionError(f"history action counts are incorrect: {action_counts}")
    entity_counts = {entity: sum(item["entityType"] == entity for item in items)
                     for entity in ("Material", "Category", "Manufacturer", "SheetSize")}
    if entity_counts != {"Material": 3, "Category": 3, "Manufacturer": 3, "SheetSize": 3}:
        raise AssertionError(f"history entity counts are incorrect: {entity_counts}")
    if not all(f"#{item['entityId']}" in item["message"] for item in items):
        raise AssertionError("history message does not contain its entity id")
    expected_deleted_names = {
        "Category": updated_category_name,
        "SheetSize": updated_size_name,
        "Manufacturer": updated_manufacturer_name,
        "Material": updated_material_name,
    }
    for item in (item for item in items if item["actionType"] == "Delete"):
        if expected_deleted_names[item["entityType"]] not in item["message"]:
            raise AssertionError("history delete event does not contain the pre-delete snapshot")

    if history(search=history_prefix, actionType="Create")["totalCount"] != 4:
        raise AssertionError("history action filter failed")
    if history(search=history_prefix, entityType="Material")["totalCount"] != 3:
        raise AssertionError("history entity filter failed")
    if history(search=history_prefix, userPhone=manager_phone)["totalCount"] != 12:
        raise AssertionError("history phone filter failed")
    if history(search=history_prefix, **{"from": "2000-01-01T00:00:00Z", "to": "2100-01-01T00:00:00Z"})["totalCount"] != 12:
        raise AssertionError("history date filters failed")
    if history(search=history_prefix.upper())["totalCount"] != 12:
        raise AssertionError("case-insensitive history search failed")
    first_page = history(search=history_prefix, page_size=1)
    second_page = history(search=history_prefix, page=2, page_size=1)
    if first_page["totalCount"] != 12 or len(first_page["items"]) != 1 or len(second_page["items"]) != 1:
        raise AssertionError("history pagination failed")
    if first_page["items"][0]["id"] == second_page["items"][0]["id"]:
        raise AssertionError("history pages contain the same item")
    expect(request("GET", "/api/catalog/history?pageSize=0", auth_cookie=manager_cookie)[0],
           400, "history zero page size validation")
    ordered_keys = [(item["occurredAt"], item["id"]) for item in items]
    if ordered_keys != sorted(ordered_keys, reverse=True):
        raise AssertionError("history sorting is incorrect")


def main():
    global cookie, history_run_id, primary_sheet_size_id, primary_manufacturer_id
    prefix = "codex-http-" + time.strftime("%Y%m%d-%H%M%S") + "-" + uuid.uuid4().hex[:6]
    dimension_base = 10000 + int(uuid.uuid4().hex[:4], 16)
    png = b"\x89PNG\r\n\x1a\n" + b"catalog-png-test"
    jpeg = b"\xff\xd8\xff\xe0" + b"catalog-jpeg-test" + b"\xff\xd9"
    webp = b"RIFF" + (20).to_bytes(4, "little") + b"WEBP" + b"catalog-webp-test"

    start_server()
    openapi_response = request("GET", "/swagger/v1/swagger.json")
    expect(openapi_response[0], 200, "OpenAPI document")
    openapi_document = json_body(openapi_response)
    sheet_size_schema = openapi_document["components"]["schemas"]["SheetSizeDto"]
    required_sheet_size_properties = {"id", "name", "height", "width", "showInFilters", "orderByCol"}
    if not required_sheet_size_properties.issubset(sheet_size_schema.get("properties", {})):
        raise AssertionError("SheetSizeDto OpenAPI properties are incomplete")
    if not required_sheet_size_properties.issubset(set(sheet_size_schema.get("required", []))):
        raise AssertionError("SheetSizeDto OpenAPI required properties are incomplete")
    mark("DOC-01", "SheetSizeDto OpenAPI schema contains all response properties and required fields")
    cleanup_operation = openapi_document["paths"]["/api/catalog/history/test-runs/{runId}"]["delete"]
    cleanup_parameters = cleanup_operation.get("parameters", [])
    if len(cleanup_parameters) != 1 or cleanup_parameters[0].get("name") != "runId":
        raise AssertionError("history cleanup OpenAPI path parameter is incorrect")
    cleanup_parameter_schema = cleanup_parameters[0].get("schema", {})
    cleanup_parameter_types = cleanup_parameter_schema.get("type", [])
    if isinstance(cleanup_parameter_types, str):
        cleanup_parameter_types = [cleanup_parameter_types]
    if "integer" not in cleanup_parameter_types or cleanup_parameter_schema.get("format") != "int32":
        raise AssertionError("history cleanup OpenAPI path parameter type is incorrect")
    if cleanup_parameters[0].get("description") != "Числовой id тестового HIST-прогона":
        raise AssertionError("history cleanup OpenAPI path parameter description is incorrect")
    if not {"200", "400", "401", "403"}.issubset(cleanup_operation.get("responses", {})):
        raise AssertionError("history cleanup OpenAPI responses are incomplete")
    mark("DOC-02", "history cleanup OpenAPI path parameter and responses are documented")

    credentials = load_test_credentials()
    user_cookie = signin(credentials["user"], credentials["password"], "user")
    tester_cookie = signin(credentials["tester"], credentials["password"], "tester")
    manager_cookie = signin(credentials["manager"], credentials["password"], "manager")
    admin_cookie = signin(credentials["administrator"], credentials["password"], "administrator")
    cookie = tester_cookie
    role_probes = [
        ("GET", "/api/materials/admin", 200),
        ("GET", "/api/catalog/history", 200),
        ("DELETE", "/api/materials/categories/2147483647", 400),
        ("DELETE", "/api/materials/sheet-sizes/2147483647", 400),
        ("DELETE", "/api/materials/manufacturers/2147483647", 400),
    ]
    for method, path, authorized_status in role_probes:
        expect(request(method, path, auth_cookie=user_cookie)[0], 403, f"ordinary user {method} {path}")
        expect(request(method, path, auth_cookie=tester_cookie)[0], authorized_status, f"tester {method} {path}")
        expect(request(method, path, auth_cookie=admin_cookie)[0], authorized_status, f"administrator {method} {path}")
    expect(request("GET", "/api/materials/admin", auth_cookie=manager_cookie)[0], 200, "manager")
    expect(request("GET", "/api/catalog/history")[0], 401, "anonymous history")
    expect(request("GET", "/api/catalog/history", auth_cookie=user_cookie)[0], 403, "ordinary user history")
    expect(request("GET", "/api/catalog/history?pageSize=201", auth_cookie=manager_cookie)[0], 400, "history page size validation")
    cleanup_probe_path = "/api/catalog/history/test-runs/1"
    expect(request("DELETE", cleanup_probe_path)[0], 401, "anonymous history cleanup")
    expect(request("DELETE", cleanup_probe_path, auth_cookie=user_cookie)[0], 403, "ordinary user history cleanup")
    expect(request("DELETE", cleanup_probe_path, auth_cookie=manager_cookie)[0], 403, "manager history cleanup")
    expect(request("DELETE", cleanup_probe_path, auth_cookie=admin_cookie)[0], 403, "administrator history cleanup")
    expect(request("DELETE", "/api/catalog/history/test-runs/not-an-int",
                   auth_cookie=tester_cookie)[0], 400, "history cleanup run id validation")
    tester_cleanup_probe = request("DELETE", cleanup_probe_path, auth_cookie=tester_cookie)
    expect(tester_cleanup_probe[0], 200, "tester history cleanup")
    if json_body(tester_cleanup_probe)["deletedCount"] != 0:
        raise AssertionError("history cleanup probe deleted unrelated events")
    mark("PREP-01—PREP-03", "user, tester, manager and administrator cookies received; anonymous client prepared")
    mark("AUTH-08—AUTH-12", "protected access and tester-only history cleanup authorization passed")

    tester_probe_name = prefix + "-tester-no-history"
    tester_create = request("POST", "/api/materials/manufacturers", {"name": tester_probe_name}, auth_cookie=tester_cookie)
    expect(tester_create[0], 200, "tester create manufacturer without history")
    tester_probe_id = json_body(tester_create)["id"]
    created_manufacturers.append(tester_probe_id)
    expect(request("PUT", f"/api/materials/manufacturers/{tester_probe_id}", {
        "name": tester_probe_name + "-updated"
    }, auth_cookie=tester_cookie)[0], 200, "tester update manufacturer without history")
    expect(request("DELETE", f"/api/materials/manufacturers/{tester_probe_id}", auth_cookie=tester_cookie)[0], 200,
           "tester delete manufacturer without history")
    created_manufacturers.remove(tester_probe_id)
    if history(search=tester_probe_name)["totalCount"] != 0:
        raise AssertionError("tester operations created catalog history events")
    mark("HIST-13", "tester create/update/delete succeeded without catalog history events")

    primary_sheet_size_id = create_sheet_size(prefix + "-size-a", dimension_base, dimension_base + 1)
    secondary_sheet_size_id = create_sheet_size(prefix + "-size-b", dimension_base + 2, dimension_base + 3)
    expect(request("GET", "/api/materials/sheet-sizes")[0], 200, "public material sheet sizes")
    expect(request("GET", f"/api/materials/sheet-sizes/{primary_sheet_size_id}")[0], 200, "public material sheet size card")
    expect(request("POST", "/api/materials/sheet-sizes", {"name": prefix + "-other", "height": dimension_base, "width": dimension_base + 1}, auth=True)[0], 400, "duplicate material sheet size")
    expect(request("POST", "/api/materials/sheet-sizes", {"name": prefix + "-size-a", "height": dimension_base + 4, "width": dimension_base + 5}, auth=True)[0], 400, "duplicate material sheet size name")
    temporary_sheet_size_id = create_sheet_size(prefix + "-size-temp", dimension_base + 6, dimension_base + 7)
    expect(request("PUT", f"/api/materials/sheet-sizes/{temporary_sheet_size_id}", {"name": prefix + "-size-updated", "height": dimension_base + 8, "width": dimension_base + 9}, auth=True)[0], 200, "update material sheet size")
    expect(request("PUT", f"/api/materials/sheet-sizes/{temporary_sheet_size_id}", {"name": prefix + "-size-updated", "height": dimension_base + 8, "width": dimension_base + 9}, auth=True)[0], 200, "no-op material sheet size update")
    expect(request("DELETE", f"/api/materials/sheet-sizes/{temporary_sheet_size_id}", auth=True)[0], 200, "delete free material sheet size")
    created_sheet_sizes.remove(temporary_sheet_size_id)
    mark("SIZE-01—SIZE-04", "create/list/card/duplicate/update/free delete passed")

    own_sizes = [x for x in sheet_sizes() if x["id"] in (primary_sheet_size_id, secondary_sheet_size_id)]
    if [x["id"] for x in own_sizes] != [primary_sheet_size_id, secondary_sheet_size_id]:
        raise AssertionError("created material sheet size order is incorrect")
    expect(request("POST", "/api/materials/sheet-sizes/change-order-col", {
        "id": secondary_sheet_size_id, "direction": "UP"
    }, auth=True)[0], 200, "move material sheet size up")
    own_sizes = [x for x in sheet_sizes() if x["id"] in (primary_sheet_size_id, secondary_sheet_size_id)]
    if [x["id"] for x in own_sizes] != [secondary_sheet_size_id, primary_sheet_size_id]:
        raise AssertionError("material sheet size was not moved up")
    expect(request("POST", "/api/materials/sheet-sizes/change-order-col", {
        "id": secondary_sheet_size_id, "direction": "DOWN"
    }, auth=True)[0], 200, "move material sheet size down")
    own_sizes = [x for x in sheet_sizes() if x["id"] in (primary_sheet_size_id, secondary_sheet_size_id)]
    if [x["id"] for x in own_sizes] != [primary_sheet_size_id, secondary_sheet_size_id]:
        raise AssertionError("material sheet size was not moved down")
    mark("SIZE-08—SIZE-09", "material sheet size moved up and down")

    primary_manufacturer_id = create_manufacturer(prefix + "-manufacturer-a")
    secondary_manufacturer_id = create_manufacturer(prefix + "-manufacturer-b")
    expect(request("GET", f"/api/materials/manufacturers/{primary_manufacturer_id}")[0], 200, "public manufacturer card")
    expect(request("POST", "/api/materials/manufacturers", {"name": prefix + "-manufacturer-a"}, auth=True)[0], 400, "duplicate manufacturer")
    temporary_manufacturer_id = create_manufacturer(prefix + "-manufacturer-temp")
    expect(request("PUT", f"/api/materials/manufacturers/{temporary_manufacturer_id}", {"name": prefix + "-manufacturer-updated"}, auth=True)[0], 200, "update manufacturer")
    expect(request("PUT", f"/api/materials/manufacturers/{temporary_manufacturer_id}", {"name": prefix + "-manufacturer-updated"}, auth=True)[0], 200, "no-op manufacturer update")
    expect(request("DELETE", f"/api/materials/manufacturers/{temporary_manufacturer_id}", auth=True)[0], 200, "delete free manufacturer")
    created_manufacturers.remove(temporary_manufacturer_id)
    expect(request("POST", "/api/materials/manufacturers/change-order-col", {"id": secondary_manufacturer_id, "direction": "UP"}, auth=True)[0], 200, "manufacturer up")
    expect(request("POST", "/api/materials/manufacturers/change-order-col", {"id": secondary_manufacturer_id, "direction": "DOWN"}, auth=True)[0], 200, "manufacturer down")
    own_manufacturers = [x["id"] for x in manufacturers() if x["id"] in (primary_manufacturer_id, secondary_manufacturer_id)]
    if own_manufacturers != [primary_manufacturer_id, secondary_manufacturer_id]:
        raise AssertionError("manufacturer sorting incorrect")
    all_manufacturers = manufacturers()
    expect(request("POST", "/api/materials/manufacturers/change-order-col", {"id": all_manufacturers[0]["id"], "direction": "UP"}, auth=True)[0], 400, "manufacturer upper boundary")
    expect(request("POST", "/api/materials/manufacturers/change-order-col", {"id": all_manufacturers[-1]["id"], "direction": "DOWN"}, auth=True)[0], 400, "manufacturer lower boundary")
    mark("MFR-01—MFR-06", "manufacturer CRUD, duplicate, sorting and boundaries passed")

    expect(request("GET", "/api/materials?calculator=Raskroy")[0], 200, "public materials")
    expect(request("GET", "/api/materials/categories")[0], 200, "public categories")
    expect(request("GET", "/api/materials/admin")[0], 401, "anonymous admin list")
    anonymous_mutations = [
        ("POST", "/api/materials/categories", {"name": "x", "externalLink": "x"}),
        ("PUT", "/api/materials/categories/1", {"name": "x", "externalLink": "x"}),
        ("DELETE", "/api/materials/categories/1", None),
        ("POST", "/api/materials/categories/change-order-col", {"categoryId": 1, "direction": "UP"}),
        ("POST", "/api/materials/sheet-sizes", {"name": "x", "height": 1, "width": 1}),
        ("POST", "/api/materials/sheet-sizes/change-order-col", {"id": 1, "direction": "UP"}),
        ("POST", "/api/materials/manufacturers", {"name": "x"}),
        ("PUT", "/api/materials/manufacturers/1", {"name": "x"}),
        ("DELETE", "/api/materials/manufacturers/1", None),
        ("POST", "/api/materials/manufacturers/change-order-col", {"id": 1, "direction": "UP"}),
    ]
    for method, path, body in anonymous_mutations:
        expect(request(method, path, body)[0], 401, f"anonymous {method} {path}")

    cat1, cat2, cat3 = [create_category(prefix + suffix) for suffix in ("-cat-a", "-cat-b", "-cat-c")]
    listed = categories()
    own = [x for x in listed if x["id"] in (cat1, cat2, cat3)]
    if [x["id"] for x in own] != [cat1, cat2, cat3]:
        raise AssertionError("created category order is incorrect")
    for category_id in (cat1, cat2, cat3):
        response = request("GET", f"/api/materials/categories/{category_id}")
        expect(response[0], 200, "public category card")
    expect(request("POST", "/api/materials/categories", {
        "name": prefix + "-cat-a", "externalLink": "https://example.test/duplicate"
    }, auth=True)[0], 400, "duplicate category name")
    duplicate_categories = [x for x in categories() if x["name"] == prefix + "-cat-a"]
    if len(duplicate_categories) != 1:
        raise AssertionError("duplicate category was created")
    response = request("PUT", f"/api/materials/categories/{cat2}", {
        "name": prefix + "-cat-b-updated", "externalLink": "https://example.test/updated"
    }, auth=True)
    expect(response[0], 200, "update category")
    expect(request("PUT", f"/api/materials/categories/{cat2}", {
        "name": prefix + "-cat-b-updated", "externalLink": "https://example.test/updated"
    }, auth=True)[0], 200, "no-op category update")
    updated = json_body(request("GET", f"/api/materials/categories/{cat2}"))
    if updated["name"] != prefix + "-cat-b-updated":
        raise AssertionError("category update not visible")
    expect(request("POST", "/api/materials/categories/change-order-col", {"categoryId": cat2, "direction": "UP"}, auth=True)[0], 200, "category up")
    own_ids = [x["id"] for x in categories() if x["id"] in (cat1, cat2, cat3)]
    if own_ids != [cat2, cat1, cat3]:
        raise AssertionError("category UP sorting incorrect")
    expect(request("POST", "/api/materials/categories/change-order-col", {"categoryId": cat2, "direction": "DOWN"}, auth=True)[0], 200, "category down")
    all_categories = categories()
    expect(request("POST", "/api/materials/categories/change-order-col", {"categoryId": all_categories[0]["id"], "direction": "UP"}, auth=True)[0], 400, "category upper boundary")
    expect(request("POST", "/api/materials/categories/change-order-col", {"categoryId": all_categories[-1]["id"], "direction": "DOWN"}, auth=True)[0], 400, "category lower boundary")
    mark("CAT-01—CAT-08", "create/list/cards/duplicate protection/update/sorting and both boundaries passed")

    payload, headers = multipart("file", "anon.png", png, "image/png")
    expect(request("POST", "/api/materials/images", payload, headers=headers, raw=True)[0], 401, "anonymous image upload")
    images = []
    for filename, content, content_type in (("test.png", png, "image/png"), ("test.jpg", jpeg, "image/jpeg"), ("test.webp", webp, "image/webp")):
        response = upload(filename, content, content_type)
        expect(response[0], 200, f"upload {filename}")
        body = json_body(response)
        if body["contentType"] != content_type:
            raise AssertionError(f"wrong content type for {filename}")
        images.append((body["fileGuid"], content, content_type))
        guids.append(body["fileGuid"])
    expect(upload("empty.png", b"", "image/png")[0], 400, "empty image")
    expect(upload("bad.txt", b"not an image", "text/plain")[0], 400, "unsupported image")
    expect(upload("large.png", b"\x89PNG\r\n\x1a\n" + b"x" * (5 * 1024 * 1024), "image/png")[0], 400, "oversized image")
    for guid, content, content_type in images:
        response = request("GET", "/api/materials/images/" + guid)
        expect(response[0], 200, "uploaded image GET")
        if response[1].get("Content-Type") != content_type or response[2] != content:
            raise AssertionError("uploaded image bytes/content-type mismatch")
    mark("IMG-01—IMG-04", "auth, three formats, validation and pre-binding reads passed")

    restart_server()
    response = request("GET", "/api/materials/images/" + images[0][0])
    expect(response[0], 200, "PNG after restart")
    if response[2] != png:
        raise AssertionError("PNG changed after restart")
    mark("IMG-05", "uploaded PNG survived restart")

    m1_model = material_model(prefix + "-mat-raskroy", cat1, "raskroy", images[0][0])
    m2_model = material_model(prefix + "-mat-pvh", cat1, "pvhFacades")
    m3_model = material_model(prefix + "-mat-emal", cat2, "emalFacades")
    m1, m2, m3 = [create_material(x) for x in (m1_model, m2_model, m3_model)]
    invalid_manufacturer_model = material_model(prefix + "-invalid-manufacturer", cat1, "raskroy")
    invalid_manufacturer_model["manufacturerId"] = 2147483647
    expect(request("POST", "/api/materials", invalid_manufacturer_model, auth=True)[0], 400, "invalid manufacturer id")
    zero_manufacturer_model = material_model(prefix + "-zero-manufacturer", cat1, "raskroy")
    zero_manufacturer_model["manufacturerId"] = 0
    expect(request("POST", "/api/materials", zero_manufacturer_model, auth=True)[0], 400, "zero manufacturer id")
    expect(request("GET", f"/api/materials/{m1}")[0], 200, "public material card")
    expect(request("GET", f"/api/materials/admin/{m1}", auth=True)[0], 200, "admin material card")
    material_card = json_body(request("GET", f"/api/materials/{m1}"))
    if material_card["sheetSize"]["id"] != primary_sheet_size_id:
        raise AssertionError("material sheet size absent from material card")
    if material_card["manufacturer"]["id"] != primary_manufacturer_id:
        raise AssertionError("manufacturer absent from material card")
    if "orderByCol" in material_card["sheetSize"] or "orderByCol" in material_card["category"] or "orderByCol" in material_card["manufacturer"]:
        raise AssertionError("nested material relations contain sorting fields")
    expect(request("DELETE", f"/api/materials/sheet-sizes/{primary_sheet_size_id}", auth=True)[0], 400, "delete used material sheet size")
    expect(request("DELETE", f"/api/materials/manufacturers/{primary_manufacturer_id}", auth=True)[0], 400, "delete used manufacturer")
    mark("MFR-07—MFR-08", "material relation visible; used manufacturer deletion rejected")
    mark("SIZE-05—SIZE-06", "material relation visible; used size deletion rejected")
    expect(request("GET", f"/api/materials/categories/{cat1}")[0], 200, "public category card after material")
    png_response = request("GET", "/api/materials/images/" + images[0][0])
    expect(png_response[0], 200, "bound PNG")
    if png_response[2] != png:
        raise AssertionError("bound PNG mismatch")
    mark("AUTH-01—AUTH-07", "public lists/cards/image and anonymous protected calls passed")
    mark("IMG-06—IMG-07", "material created after restart with PNG; card and image passed")

    admin_ids = {x["id"] for x in admin_materials()}
    if not {m1, m2, m3}.issubset(admin_ids):
        raise AssertionError("created materials absent from admin list")
    unfiltered_ids = {x["id"] for x in public_materials()}
    if not {m1, m2, m3}.issubset(unfiltered_ids):
        raise AssertionError("unfiltered public list omitted test materials")

    for calculator, expected_id in (("Raskroy", m1), ("PvhFacades", m2), ("EmalFacades", m3)):
        ids = {x["id"] for x in public_materials(calculator)}
        if expected_id not in ids:
            raise AssertionError(f"filter {calculator} omitted expected material")
        wrong_test_ids = ({m1, m2, m3} - {expected_id}) & ids
        if wrong_test_ids:
            raise AssertionError(f"filter {calculator} returned inapplicable test material")

    combined_ids = {x["id"] for x in public_materials("Raskroy", "PvhFacades")}
    if not {m1, m2}.issubset(combined_ids) or m3 in combined_ids:
        raise AssertionError("combined calculator filter returned incorrect test materials")
    expect(request("GET", "/api/materials?calculator=Unknown")[0], 400, "invalid calculator")

    m3_updated = copy.deepcopy(m3_model)
    m3_updated.update({
        "categoryId": cat3, "name": prefix + "-mat-emal-updated", "article": "UPDATED",
        "count": 7, "sheetSizeId": secondary_sheet_size_id, "depth": 16, "kvM": 2.9768,
        "perimetrM": 7.32, "price": 456.78, "countTypeEnum": "SHT",
        "applicableToRaskroys": True, "applicableToEmalFacades": False,
        "commentOnMaterialIsRequired": True, "allowSecondItemInOrder": False
    })
    expect(request("PUT", f"/api/materials/{m3}", m3_updated, auth=True)[0], 200, "update material")
    expect(request("PUT", f"/api/materials/{m3}", m3_updated, auth=True)[0], 200, "no-op material update")
    public_card = json_body(request("GET", f"/api/materials/{m3}"))
    admin_card = json_body(request("GET", f"/api/materials/admin/{m3}", auth=True))
    if public_card["category"]["id"] != cat3 or admin_card["price"] != 456.78 or admin_card["countTypeEnum"] != "SHT":
        raise AssertionError("updated material fields not visible")

    expect(request("POST", "/api/materials/change-order-col", {"id": m2, "direction": "UP"}, auth=True)[0], 200, "material up")
    cat1_order = [x["id"] for x in admin_materials() if x["category"]["id"] == cat1]
    if cat1_order != [m2, m1]:
        raise AssertionError("material UP sorting incorrect")
    cat3_before = [x["id"] for x in admin_materials() if x["category"]["id"] == cat3]
    expect(request("POST", "/api/materials/change-order-col", {"id": m2, "direction": "DOWN"}, auth=True)[0], 200, "material down")
    if [x["id"] for x in admin_materials() if x["category"]["id"] == cat3] != cat3_before:
        raise AssertionError("sorting affected another category")
    cat1_rows = [x for x in admin_materials() if x["category"]["id"] == cat1]
    expect(request("POST", "/api/materials/change-order-col", {"id": cat1_rows[0]["id"], "direction": "UP"}, auth=True)[0], 400, "material upper boundary")
    expect(request("POST", "/api/materials/change-order-col", {"id": cat1_rows[-1]["id"], "direction": "DOWN"}, auth=True)[0], 400, "material lower boundary")
    expect(request("DELETE", f"/api/materials/categories/{cat1}", auth=True)[0], 400, "delete used category")
    mark("CAT-09", "used category deletion rejected and entities retained")
    mark("MAT-01—MAT-08", "create/lists/cards/filters/update/sorting/isolation/boundaries passed")

    jpeg_model = copy.deepcopy(m1_model)
    jpeg_model["image"] = images[1][0]
    expect(request("PUT", f"/api/materials/{m1}", jpeg_model, auth=True)[0], 200, "replace PNG with JPEG")
    if json_body(request("GET", f"/api/materials/{m1}"))["image"] != images[1][0]:
        raise AssertionError("JPEG GUID absent from material card")
    expect(request("GET", "/api/materials/images/" + images[0][0])[0], 400, "old PNG invalidated")
    for _ in range(2):
        response = request("GET", "/api/materials/images/" + images[1][0])
        expect(response[0], 200, "repeat JPEG GET")
        if response[2] != jpeg or response[1].get("Content-Type") != "image/jpeg":
            raise AssertionError("JPEG response mismatch")
    time.sleep(1)
    expect(request("GET", "/api/materials/images/" + images[1][0])[0], 200, "later JPEG GET")
    mark("IMG-08—IMG-10", "JPEG replacement invalidated PNG; repeated and later reads passed")

    webp_model = copy.deepcopy(m1_model)
    webp_model["image"] = images[2][0]
    expect(request("PUT", f"/api/materials/{m1}", webp_model, auth=True)[0], 200, "replace JPEG with WebP")
    expect(request("GET", "/api/materials/images/" + images[1][0])[0], 400, "old JPEG invalidated")
    response = request("GET", "/api/materials/images/" + images[2][0])
    expect(response[0], 200, "WebP GET")
    if response[2] != webp or response[1].get("Content-Type") != "image/webp":
        raise AssertionError("WebP mismatch")
    mark("IMG-11", "WebP replacement invalidated JPEG and returned original bytes")

    restart_server()
    for _ in range(2):
        response = request("GET", "/api/materials/images/" + images[2][0])
        expect(response[0], 200, "WebP after restart")
        if response[2] != webp:
            raise AssertionError("WebP changed after restart")
    mark("IMG-12", "WebP returned twice after restart with original bytes")

    base_invalid = material_model(prefix + "-validation", cat2, "raskroy")
    expect(request("POST", "/api/materials", {}, auth=True)[0], 400, "empty required fields")
    for bad_category in (0, 2147483000):
        bad = copy.deepcopy(base_invalid); bad["categoryId"] = bad_category; bad["name"] += str(bad_category)
        expect(request("POST", "/api/materials", bad, auth=True)[0], 400, "bad category")
    for field, value in (("sheetSizeId", 0), ("depth", 0), ("kvM", -1), ("perimetrM", 0)):
        bad = copy.deepcopy(base_invalid); bad[field] = value; bad["name"] += "-" + field
        expect(request("POST", "/api/materials", bad, auth=True)[0], 400, "bad dimensions")
    bad = copy.deepcopy(base_invalid); bad["price"] = 0
    expect(request("POST", "/api/materials", bad, auth=True)[0], 400, "bad price")
    bad = copy.deepcopy(base_invalid)
    bad.update({"applicableToRaskroys": False, "applicableToPvhFacades": False, "applicableToEmalFacades": False})
    expect(request("POST", "/api/materials", bad, auth=True)[0], 400, "no calculator")
    duplicate = copy.deepcopy(base_invalid); duplicate["name"] = m2_model["name"]
    expect(request("POST", "/api/materials", duplicate, auth=True)[0], 400, "duplicate material name")
    expect(request("GET", "/api/materials/2147483000")[0], 400, "missing material")
    expect(request("GET", "/api/materials/categories/2147483000")[0], 400, "missing category")
    expect(request("POST", "/api/materials/change-order-col", {"id": m1, "direction": "SIDEWAYS"}, auth=True)[0], 400, "invalid direction")
    bad = copy.deepcopy(base_invalid); bad["name"] += "-guid"; bad["image"] = str(uuid.uuid4())
    expect(request("POST", "/api/materials", bad, auth=True)[0], 400, "missing image GUID")
    mark("VAL-01—VAL-10", "all negative models and missing ids/GUID returned 400")

    counter_targets = (
        ("/api/materials/categories", cat1),
        ("/api/materials/sheet-sizes", primary_sheet_size_id),
        ("/api/materials/manufacturers", primary_manufacturer_id)
    )
    counters_before_delete = [relation_counts(path, relation_id) for path, relation_id in counter_targets]
    expect(request("GET", "/api/materials/images/" + images[2][0])[0], 200, "WebP before delete")
    expect(delete_material(m1)[0], 200, "delete material")
    expect(request("GET", "/api/materials/images/" + images[2][0])[0], 400, "WebP after delete")
    expect(request("GET", f"/api/materials/{m1}")[0], 400, "deleted public card")
    expect(request("GET", f"/api/materials/admin/{m1}", auth=True)[0], 400, "deleted admin card")
    if m1 in {x["id"] for x in admin_materials()} or m1 in {x["id"] for x in public_materials("Raskroy")}:
        raise AssertionError("deleted material remained in a list")
    counters_after_delete = [relation_counts(path, relation_id) for path, relation_id in counter_targets]
    for before, after in zip(counters_before_delete, counters_after_delete):
        if after != (before[0] - 1, before[1]):
            raise AssertionError(f"physically deleted material remained in relation counters: {before} -> {after}")
    expect(delete_material(m1)[0], 400, "repeat material delete")
    for guid, _, _ in images:
        expect(request("GET", "/api/materials/images/" + guid)[0], 400, "final image GUID check")
    mark("MAT-09—MAT-11", "physical delete/list and card absence/repeat delete passed")
    mark("CAT-11, SIZE-10, MFR-11", "physically deleted material excluded from list and card counters")
    mark("IMG-13—IMG-14", "delete invalidated WebP; all three GUIDs unavailable")

    expect(delete_material(m2)[0], 200, "cleanup m2")
    expect(delete_material(m3)[0], 200, "cleanup m3")
    for category_id in (cat1, cat2, cat3):
        expect(request("DELETE", f"/api/materials/categories/{category_id}", auth=True)[0], 200, "delete free category")
        created_categories.remove(category_id)
    for size_id in (primary_sheet_size_id, secondary_sheet_size_id):
        expect(request("DELETE", f"/api/materials/sheet-sizes/{size_id}", auth=True)[0], 200, "delete free material sheet size")
        created_sheet_sizes.remove(size_id)
    for manufacturer_id in (primary_manufacturer_id, secondary_manufacturer_id):
        expect(request("DELETE", f"/api/materials/manufacturers/{manufacturer_id}", auth=True)[0], 200, "delete free manufacturer")
        created_manufacturers.remove(manufacturer_id)
    mark("MFR-08—MFR-09", "used manufacturer protected; free manufacturers deleted")
    mark("SIZE-07", "free material sheet sizes deleted")
    mark("CAT-10", "free categories deleted")
    remaining_category_ids = {x["id"] for x in categories()}
    if {cat1, cat2, cat3} & remaining_category_ids:
        raise AssertionError("test categories remained in list")
    remaining_material_ids = {x["id"] for x in admin_materials()}
    if {m1, m2, m3} & remaining_material_ids:
        raise AssertionError("test materials remained in admin list")
    for guid, _, _ in images:
        expect(request("GET", "/api/materials/images/" + guid)[0], 400, "FIN image check")
    mark("FIN-01—FIN-03", "materials/categories removed and GUIDs unavailable")

    if history(search=prefix)["totalCount"] != 0:
        raise AssertionError("tester functional scenarios created catalog history events")
    mark("HIST-13", "tester functional CRUD and cleanup created no catalog history events")

    history_run_id = int(uuid.uuid4().hex[:7], 16) + 1
    history_prefix = f"codex-history-{history_run_id}"
    run_history_scenarios(
        history_prefix,
        dimension_base,
        manager_cookie,
        tester_cookie,
        credentials["manager"])
    mark("HIST-01—HIST-12", "isolated manager CRUD audit, no-op/sorting exclusion, phone, snapshots, authorization, filters, search and pagination passed")

    cleanup_response = request(
        "DELETE",
        f"/api/catalog/history/test-runs/{history_run_id}",
        auth_cookie=tester_cookie)
    expect(cleanup_response[0], 200, "test history cleanup")
    if json_body(cleanup_response)["deletedCount"] != 12:
        raise AssertionError("test history cleanup deleted an unexpected number of events")
    if history(search=history_prefix)["totalCount"] != 0:
        raise AssertionError("test history events remained after cleanup")
    completed_history_run_id = history_run_id
    history_run_id = None
    mark("HIST-14", "tester-only endpoint deleted all 12 isolated history events and left none")

    observed_history_total_count = history()["totalCount"]

    print(json.dumps({
        "success": True,
        "prefix": prefix,
        "historyRunId": completed_history_run_id,
        "historyPrefix": history_prefix,
        "observedHistoryTotalCount": observed_history_total_count,
        "results": results
    }, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    try:
        main()
    except Exception as error:
        print(json.dumps({"success": False, "error": str(error), "results": results}, ensure_ascii=False, indent=2))
        try:
            cleanup()
        except Exception:
            pass
        sys.exit(1)
    finally:
        stop_server()
