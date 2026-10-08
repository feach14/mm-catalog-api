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
primary_material_type_id = None
guids = []
thickness_ids = {}
created_thicknesses = []
created_material_types = []


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
        raise AssertionError(f"{label}: ожидался HTTP {expected}, получен {status}")


def mark(case, detail):
    results[case] = detail
    print(f"ПРОЙДЕН {case}: {detail}", file=sys.stderr, flush=True)


def multipart(name, filename, content, content_type, fields=None):
    boundary = "----CodexCatalog" + uuid.uuid4().hex
    parts = []
    for field_name, value in (fields or {}).items():
        parts.append(
            f"--{boundary}\r\n"
            f'Content-Disposition: form-data; name="{field_name}"\r\n\r\n'
            f"{value}\r\n".encode())
    parts.append((
        f"--{boundary}\r\n"
        f'Content-Disposition: form-data; name="{name}"; filename="{filename}"\r\n'
        f"Content-Type: {content_type}\r\n\r\n"
    ).encode() + content + b"\r\n")
    payload = b"".join(parts) + f"--{boundary}--\r\n".encode()
    return payload, {"Content-Type": f"multipart/form-data; boundary={boundary}"}


def upload(filename, content, content_type, image_type="Original", auth=True):
    payload, headers = multipart(
        "file", filename, content, content_type, {"imageType": image_type})
    return request("POST", "/api/for-admin/materials/images", payload, auth=auth, headers=headers, raw=True)


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
            status, _, _ = request("GET", "/swagger/v1/swagger.json")
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
    response = request("GET", "/api/for-admin/materials/categories", auth=True)
    expect(response[0], 200, "categories list")
    return json_body(response)["items"]


def sheet_sizes():
    response = request("GET", "/api/for-admin/materials/sheet-sizes", auth=True)
    expect(response[0], 200, "material sheet sizes list")
    return json_body(response)["items"]


def manufacturers():
    response = request("GET", "/api/for-admin/materials/manufacturers", auth=True)
    expect(response[0], 200, "manufacturers list")
    return json_body(response)["items"]


def history(**filters):
    parameters = {"page": filters.pop("page", 1), "pageSize": filters.pop("page_size", 200)}
    parameters.update({key: value for key, value in filters.items() if value is not None})
    response = request("GET", "/api/for-admin/catalog/history?" + urllib.parse.urlencode(parameters), auth=True)
    expect(response[0], 200, "catalog history")
    return json_body(response)


def relation_counts(path, relation_id):
    list_response = request("GET", path, auth=path.startswith("/api/for-admin/"))
    expect(list_response[0], 200, f"{path} list for counters")
    list_item = next(item for item in json_body(list_response)["items"] if item["id"] == relation_id)
    card_response = request("GET", f"{path}/{relation_id}", auth=path.startswith("/api/for-admin/"))
    expect(card_response[0], 200, f"{path} card for counters")
    card = json_body(card_response)
    list_counts = (list_item["materialsAnyCount"], list_item["materialsNotAnyCount"])
    equal(card["id"], relation_id, "Dictionary card ID")
    return list_counts


def admin_materials():
    response = request("GET", "/api/for-admin/materials", auth=True)
    expect(response[0], 200, "admin material list")
    return json_body(response)["items"]


def public_materials():
    response = request("GET", "/api/materials")
    expect(response[0], 200, "Полный публичный список материалов")
    return json_body(response)["items"]


def create_category(name, hide_on_site=False):
    response = request("POST", "/api/for-admin/materials/categories", {
        "name": name,
        "externalLink": "https://example.test/" + name,
        "hideOnSite": hide_on_site
    }, auth=True)
    expect(response[0], 200, "create category")
    category_id = json_body(response)["id"]
    created_categories.append(category_id)
    return category_id


def create_sheet_size(name, height, width):
    response = request("POST", "/api/for-admin/materials/sheet-sizes", {
        "name": name, "height": height, "width": width
    }, auth=True)
    expect(response[0], 200, "create material sheet size")
    size_id = json_body(response)["id"]
    created_sheet_sizes.append(size_id)
    return size_id


def create_manufacturer(name):
    response = request("POST", "/api/for-admin/materials/manufacturers", {"name": name}, auth=True)
    expect(response[0], 200, "create manufacturer")
    manufacturer_id = json_body(response)["id"]
    created_manufacturers.append(manufacturer_id)
    return manufacturer_id


def material_model(name, category_id, flag, image=None):
    return {
        "categoryId": category_id,
        "sheetSizeId": primary_sheet_size_id,
        "manufacturerId": primary_manufacturer_id,
        "materialTypeId": primary_material_type_id,
        "name": name,
        "article": "ART-" + name[-8:],
        "image": image,
        "count": 2,
        "thicknessId": thickness_ids[18],
        "kvM": 5.796,
        "perimetrM": 9.74,
        "applicableToCutting": flag == "raskroy",
        "applicableToPvhFacades": flag == "pvhFacades",
        "applicableToEnamelFacades": flag == "emalFacades",
        "commentOnMaterialIsRequired": False,
        "allowSecondItemInOrder": True,
        "externalLink": "https://example.test/material",
        "price": 123.45,
        "hideOnSite": False,
        "hidePriceOnSite": False,
        "countTypeEnum": "M2"
    }


def create_material(model):
    response = request("POST", "/api/for-admin/materials", model, auth=True)
    expect(response[0], 200, "create material")
    material_id = json_body(response)["id"]
    created_materials.append(material_id)
    return material_id


def delete_material(material_id):
    response = request("DELETE", f"/api/for-admin/materials/{material_id}", auth=True)
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
            response = request("DELETE", f"/api/for-admin/materials/categories/{category_id}", auth=True)
            if response[0] == 200:
                created_categories.remove(category_id)
        except Exception:
            pass
    for size_id in list(reversed(created_sheet_sizes)):
        try:
            response = request("DELETE", f"/api/for-admin/materials/sheet-sizes/{size_id}", auth=True)
            if response[0] == 200:
                created_sheet_sizes.remove(size_id)
        except Exception:
            pass
    for manufacturer_id in list(reversed(created_manufacturers)):
        try:
            response = request("DELETE", f"/api/for-admin/materials/manufacturers/{manufacturer_id}", auth=True)
            if response[0] == 200:
                created_manufacturers.remove(manufacturer_id)
        except Exception:
            pass
    for thickness_id in list(reversed(created_thicknesses)):
        response = request("DELETE", f"/api/for-admin/materials/thicknesses/{thickness_id}", auth=True)
        if response[0] == 200:
            created_thicknesses.remove(thickness_id)
    for entity_id in list(created_material_types):
        expect(request("DELETE", f"/api/for-admin/materials/material-types/{entity_id}", auth=True)[0], 200, "Очистка типа")
        created_material_types.remove(entity_id)
    if history_run_id:
        try:
            request("DELETE", f"/api/for-admin/catalog/history/test-runs/{history_run_id}", auth=True)
        except Exception:
            pass


def run_history_scenarios(history_prefix, dimension_base, manager_cookie, tester_cookie, manager_phone):
    category_name = history_prefix + "-category"
    category_model = {"name": category_name, "externalLink": "https://example.test/history-category"}
    response = request("POST", "/api/for-admin/materials/categories", category_model, auth_cookie=manager_cookie)
    expect(response[0], 200, "history category create")
    category_id = json_body(response)["id"]
    created_categories.append(category_id)

    size_name = history_prefix + "-size"
    size_model = {"name": size_name, "height": dimension_base + 100, "width": dimension_base + 101}
    response = request("POST", "/api/for-admin/materials/sheet-sizes", size_model, auth_cookie=manager_cookie)
    expect(response[0], 200, "history sheet size create")
    size_id = json_body(response)["id"]
    created_sheet_sizes.append(size_id)

    manufacturer_name = history_prefix + "-manufacturer"
    response = request("POST", "/api/for-admin/materials/manufacturers", {
        "name": manufacturer_name
    }, auth_cookie=manager_cookie)
    expect(response[0], 200, "history manufacturer create")
    manufacturer_id = json_body(response)["id"]
    created_manufacturers.append(manufacturer_id)

    material_name = history_prefix + "-material"
    material = material_model(material_name, category_id, "raskroy")
    material.update({"sheetSizeId": size_id, "manufacturerId": manufacturer_id})
    response = request("POST", "/api/for-admin/materials", material, auth_cookie=manager_cookie)
    expect(response[0], 200, "history material create")
    material_id = json_body(response)["id"]
    created_materials.append(material_id)

    created_history = history(search=history_prefix)
    if created_history["totalCount"] != 4:
        raise AssertionError("history create events are not isolated")

    updated_category_name = category_name + "-updated"
    updated_category_model = dict(category_model, name=updated_category_name)
    expect(request("PUT", f"/api/for-admin/materials/categories/{category_id}", updated_category_model,
                   auth_cookie=manager_cookie)[0], 200, "history category update")

    updated_size_name = size_name + "-updated"
    updated_size_model = dict(size_model, name=updated_size_name)
    expect(request("PUT", f"/api/for-admin/materials/sheet-sizes/{size_id}", updated_size_model,
                   auth_cookie=manager_cookie)[0], 200, "history sheet size update")

    updated_manufacturer_name = manufacturer_name + "-updated"
    updated_manufacturer_model = {"name": updated_manufacturer_name}
    expect(request("PUT", f"/api/for-admin/materials/manufacturers/{manufacturer_id}", updated_manufacturer_model,
                   auth_cookie=manager_cookie)[0], 200, "history manufacturer update")

    updated_material_name = material_name + "-updated"
    updated_material = copy.deepcopy(material)
    updated_material["name"] = updated_material_name
    expect(request("PUT", f"/api/for-admin/materials/{material_id}", updated_material,
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

    expect(request("PUT", f"/api/for-admin/materials/categories/{category_id}", updated_category_model,
                   auth_cookie=manager_cookie)[0], 200, "history category no-op")
    expect(request("PUT", f"/api/for-admin/materials/sheet-sizes/{size_id}", updated_size_model,
                   auth_cookie=manager_cookie)[0], 200, "history sheet size no-op")
    expect(request("PUT", f"/api/for-admin/materials/manufacturers/{manufacturer_id}", updated_manufacturer_model,
                   auth_cookie=manager_cookie)[0], 200, "history manufacturer no-op")
    expect(request("PUT", f"/api/for-admin/materials/{material_id}", updated_material,
                   auth_cookie=manager_cookie)[0], 200, "history material no-op")
    if history(search=history_prefix)["totalCount"] != 8:
        raise AssertionError("no-op update created a history event")

    helper_name = history_prefix + "-sort-helper"
    response = request("POST", "/api/for-admin/materials/categories", {
        "name": helper_name, "externalLink": "https://example.test/history-sort-helper"
    }, auth_cookie=tester_cookie)
    expect(response[0], 200, "history sorting helper create")
    helper_id = json_body(response)["id"]
    created_categories.append(helper_id)
    expect(request("POST", "/api/for-admin/materials/categories/change-order-col", {
        "categoryId": helper_id, "direction": "UP"
    }, auth_cookie=manager_cookie)[0], 200, "history sorting up")
    expect(request("POST", "/api/for-admin/materials/categories/change-order-col", {
        "categoryId": helper_id, "direction": "DOWN"
    }, auth_cookie=manager_cookie)[0], 200, "history sorting down")
    expect(request("DELETE", f"/api/for-admin/materials/categories/{helper_id}",
                   auth_cookie=tester_cookie)[0], 200, "history sorting helper cleanup")
    created_categories.remove(helper_id)
    if history(search=history_prefix)["totalCount"] != 8:
        raise AssertionError("sorting or tester cleanup created a history event")

    expect(request("DELETE", f"/api/for-admin/materials/{material_id}", auth_cookie=manager_cookie)[0],
           200, "history material delete")
    created_materials.remove(material_id)
    expect(request("DELETE", f"/api/for-admin/materials/categories/{category_id}", auth_cookie=manager_cookie)[0],
           200, "history category delete")
    created_categories.remove(category_id)
    expect(request("DELETE", f"/api/for-admin/materials/sheet-sizes/{size_id}", auth_cookie=manager_cookie)[0],
           200, "history sheet size delete")
    created_sheet_sizes.remove(size_id)
    expect(request("DELETE", f"/api/for-admin/materials/manufacturers/{manufacturer_id}", auth_cookie=manager_cookie)[0],
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
    expect(request("GET", "/api/for-admin/catalog/history?pageSize=0", auth_cookie=manager_cookie)[0],
           400, "history zero page size validation")
    ordered_keys = [(item["occurredAt"], item["id"]) for item in items]
    if ordered_keys != sorted(ordered_keys, reverse=True):
        raise AssertionError("history sorting is incorrect")


def public_query(endpoint, parameters):
    response = request("GET", "/api/materials/" + endpoint + "?" + urllib.parse.urlencode(parameters, doseq=True))
    expect(response[0], 200, f"{endpoint} {parameters}")
    return json_body(response)


def equal(actual, expected, label):
    if actual != expected:
        raise AssertionError(f"{label}: expected {expected!r}, got {actual!r}")


def problem(response, label, field=None):
    expect(response[0], 400, label)
    body = json_body(response)
    equal(body.get("status"), 400, label + " ProblemDetails status")
    if not body.get("title"):
        raise AssertionError(label + ": missing ProblemDetails title")
    if field and not any(field.lower() in key.lower() for key in body.get("errors", {})):
        raise AssertionError(label + ": missing field validation error for " + field)


def run_search_facet_scenarios(prefix, dimension_base):
    marker = prefix + "-search"
    cats = [create_category(marker + f"-cat-{i}") for i in range(3)]
    makers = [create_manufacturer(marker + f"-maker-{i}") for i in range(3)]
    sizes = [create_sheet_size(marker + f"-size-{i}", dimension_base + 20 + i * 2,
                               dimension_base + 21 + i * 2) for i in range(3)]
    for i, size_id in enumerate(sizes):
        expect(request("PUT", f"/api/for-admin/materials/sheet-sizes/{size_id}", {
            "name": marker + f"-size-{i}", "height": dimension_base + 20 + i * 2,
            "width": dimension_base + 21 + i * 2, "showInFilters": i != 1
        }, auth=True)[0], 200, "configure facet size")
    rows = []
    for i, letter in enumerate("ABCDE"):
        model = material_model(marker + "-" + letter, cats[i % 2], "raskroy" if i % 2 == 0 else "pvhFacades")
        model.update(manufacturerId=makers[(i // 2) % 2], sheetSizeId=sizes[i % 2],
                     thicknessId=thickness_ids[16 + 2 * (i % 2)], count=i % 2, article=marker + f"-article-{i}")
        rows.append(dict(model, depth=16 + 2 * (i % 2), id=create_material(model)))

    def matches(row, params, excluded=None):
        search = params.get("search", "").strip().lower()
        if search not in row["name"].lower() and search not in row["article"].lower():
            return False
        for key in ("categoryIds", "manufacturerIds", "sheetSizeIds", "depths", "thicknessIds"):
            if key != excluded and not (excluded == "depths" and key == "thicknessIds") and params.get(key) and row[key[:-1] if (key.endswith("Ids") or key == "depths") else key] not in params[key]:
                return False
        if excluded != "inStock" and "inStock" in params:
            if (row["count"] > 0) != (params["inStock"] == "true"):
                return False
        flags = {"Raskroy": "applicableToCutting", "PvhFacades": "applicableToPvhFacades"}
        return not params.get("calculators") or any(row[flags[c]] for c in params["calculators"])

    def search_check(params):
        body = public_query("search", params)
        expected = {r["id"] for r in rows if matches(r, params)}
        equal({r["id"] for r in body["items"]}, expected, "search IDs")
        equal(len(body["items"]), len(expected), "search duplicates")
        equal(body["totalCount"], len(expected), "search total")
        return body

    for term in (rows[0]["name"], rows[1]["article"]):
        search_check({"search": term})
        search_check({"search": "  " + term.upper() + "  "})
    mark("SEARCH-01", "Проверены поиск по названию и артикулу, независимость от регистра и удаление пробелов по краям")
    for symbol in ("%", "_", "\\"):
        model = material_model(prefix + "-literal-" + symbol, cats[0], "raskroy")
        material_id = create_material(model)
        control = material_model(prefix + "-literal-X" + str(ord(symbol)), cats[0], "raskroy")
        control_id = create_material(control)
        body = public_query("search", {"search": model["name"]})
        equal([r["id"] for r in body["items"]], [material_id], "literal search")
        expect(delete_material(material_id)[0], 200, "literal cleanup")
        expect(delete_material(control_id)[0], 200, "literal control cleanup")
    mark("SEARCH-02", "Спецсимволы шаблонов LIKE обрабатываются буквально")
    choices = {"categoryIds": cats[:2], "manufacturerIds": makers[:2], "sheetSizeIds": sizes[:2],
               "depths": [16, 18], "thicknessIds": [thickness_ids[16], thickness_ids[18]], "calculators": ["Raskroy", "PvhFacades"]}
    for key, values in choices.items():
        search_check({"search": marker, key: values + values})
    for stock in ("true", "false"):
        search_check({"search": marker, **choices, "categoryIds": [cats[0]], "inStock": stock})
    mark("SEARCH-03", "Проверены условия ИЛИ и И, повторяющиеся значения и наличие")
    for sort, expected in (("NameAsc", rows), ("NameDesc", list(reversed(rows))),
                           ("CatalogOrder", [rows[i] for i in (0, 2, 4, 1, 3)])):
        collected = []
        for page, length in ((1, 2), (2, 2), (3, 1), (4, 0)):
            params = {"search": marker, "sort": sort, "pageSize": 2, "page": page}
            body = public_query("search", params)
            equal((body["totalCount"], body["page"], body["pageSize"], len(body["items"])),
                  (5, page, 2, length), "page metadata")
            equal(public_query("search", params), body, "repeat page")
            collected.extend(r["id"] for r in body["items"])
        equal(collected, [r["id"] for r in expected], sort)
    mark("SEARCH-04", "Проверены все страницы и пустая страница за пределами выдачи")
    mark("SEARCH-05", "Все способы сортировки сохраняют порядок при переходе между страницами")
    for params in ({"page": 0}, {"page": -1}, {"pageSize": 0}, {"pageSize": 97},
                   {"page": 2147483647, "pageSize": 96}):
        problem(request("GET", "/api/materials/search?" + urllib.parse.urlencode(params)), "page boundary")
    for size in (1, 96):
        body = public_query("search", {"search": marker, "pageSize": size})
        equal(len(body["items"]), min(size, 5), "valid page size")
    mark("SEARCH-06", "Проверены границы пагинации и защита от переполнения")
    for endpoint in ("search", "filters"):
        public_query(endpoint, {"search": "x" * 250})
        invalid = [{"search": "x" * 251}]
        for key, values in choices.items():
            public_query(endpoint, {"search": marker, key: [values[0]] * 100})
            invalid.append({key: [values[0]] * 101})
        for key in ("categoryIds", "manufacturerIds", "sheetSizeIds", "thicknessIds"):
            invalid.extend({key: value} for value in (0, 2147483647))
        invalid.extend({"depths": value} for value in (0, -1, "NaN", "Infinity"))
        invalid.append({"calculators": "Unknown"})
        if endpoint == "search":
            invalid.append({"sort": "Unknown"})
        for params in invalid:
            problem(request("GET", "/api/materials/" + endpoint + "?" + urllib.parse.urlencode(params, doseq=True)),
                    f"{endpoint} invalid {next(iter(params))}", next(iter(params)))
    mark("SEARCH-07", "Проверены валидаторы и ограничения обоих методов API")
    empty = public_query("search", {"search": marker + "-missing", "page": 2, "pageSize": 3})
    equal(empty, {"items": [], "totalCount": 0, "page": 2, "pageSize": 3}, "empty search")
    mark("SEARCH-08", "Проверен контракт пустой выдачи")

    groups = {"categories": ("categoryIds", "id", cats), "manufacturers": ("manufacturerIds", "id", makers),
              "sheetSizes": ("sheetSizeIds", "id", sizes), "depths": ("depths", "value", [16, 18])}

    def facets_check(params):
        body = public_query("filters", params)
        for group, (field, key, values) in groups.items():
            expected = {}
            for value in sorted(set(values + params.get(field, []))):
                count = sum(matches(r, params, field) and r[field[:-1] if (field.endswith("Ids") or field == "depths") else field] == value for r in rows)
                visible = field != "sheetSizeIds" or value != sizes[1]
                if (count and visible) or value in params.get(field, []):
                    expected[value] = count
            actual = body[group]
            equal({item[key]: item["count"] for item in actual}, expected, group + " counts")
            equal([item[key] for item in actual], list(expected), group + " order/duplicates")
        counts = {value: sum(matches(r, params, "inStock") and (r["count"] > 0) == value for r in rows)
                  for value in (True, False)}
        equal({item["value"]: item["count"] for item in body["availability"]}, counts, "availability")
        equal(len(body["availability"]), 2, "availability unique")
        return body

    for field in ("categoryIds", "manufacturerIds", "sheetSizeIds", "depths"):
        facets_check({"search": marker, field: [choices[field][0]]})
    mark("FACET-01", "При расчёте каждой группы исключается её собственный фильтр")
    facets_check({"search": marker, "categoryIds": [cats[0]], "manufacturerIds": [makers[0]],
                  "sheetSizeIds": [sizes[0]], "depths": [16], "calculators": ["Raskroy"], "inStock": "false"})
    mark("FACET-02", "Счётчики при совместных ограничениях сверены с исходными тестовыми данными")
    facets_check({"search": marker, "categoryIds": [cats[2]], "manufacturerIds": [makers[2]],
                  "sheetSizeIds": [sizes[2]], "depths": [99]})
    mark("FACET-03", "Выбранные значения с нулевыми счётчиками сохранены")
    facets_check({"search": marker})
    facets_check({"search": marker, "sheetSizeIds": [sizes[1]]})
    search_check({"search": marker, "sheetSizeIds": [sizes[1]]})
    mark("FACET-04", "Скрытый размер отсутствует среди вариантов до выбора, но доступен для поиска")
    for stock in ("true", "false"):
        facets_check({"search": marker, "inStock": stock})
    rows[0]["count"] = 1
    expect(request("PUT", f'/api/for-admin/materials/{rows[0]["id"]}',
                   {k: v for k, v in rows[0].items() if k not in ("id", "depth")}, auth=True)[0], 200, "change stock")
    for stock in ("true", "false"):
        facets_check({"search": marker, "inStock": stock})
    mark("FACET-05", "Проверены изменение наличия и счётчики обоих вариантов наличия")
    facets_check({"search": marker, **{key: values * 2 for key, values in choices.items()}})
    mark("FACET-06", "Проверены порядок вариантов фильтров и повторяющиеся выбранные значения")
    facets_check({"search": marker + "-missing"})
    facets_check({"search": marker + "-missing", "categoryIds": [cats[0]]})
    mark("FACET-07", "Проверены пустые группы фильтров и выбранная категория с нулевым счётчиком")
    for row in rows:
        expect(delete_material(row["id"])[0], 200, "search fixture cleanup")
    for ids, path, tracked in ((cats, "categories", created_categories), (sizes, "sheet-sizes", created_sheet_sizes),
                               (makers, "manufacturers", created_manufacturers)):
        for entity_id in ids:
            expect(request("DELETE", f"/api/for-admin/materials/{path}/{entity_id}", auth=True)[0], 200, "facet fixture cleanup")
            tracked.remove(entity_id)


def run_variant_scenarios(prefix, category_id, png):
    fields = {"image": ("Original", "original"), "thumbnail240": ("Thumbnail240", "thumbnail240"),
              "thumbnail480": ("Thumbnail480", "thumbnail480")}
    uploaded = {}
    model = material_model(prefix + "-variants-A", category_id, "raskroy")
    material_id = None

    def new_image(field):
        kind, _ = fields[field]
        content = png + uuid.uuid4().bytes
        response = upload(prefix + "-variant.png", content, "image/png", kind)
        expect(response[0], 200, "variant upload")
        value = json_body(response)
        guid = value["fileGuid"]
        uploaded[guid] = (field, content)
        equal(value["imageType"], kind, "upload image type")
        equal(value["contentType"], "image/png", "upload MIME")
        return guid

    def file_check(guid):
        response = request("GET", "/api/materials/images/" + guid)
        expect(response[0], 200, "variant file")
        equal(response[2], uploaded[guid][1], "variant bytes")
        equal({k.lower(): v for k, v in response[1].items()}["content-type"], "image/png", "variant MIME")

    def snapshots(entity_id):
        cards = []
        for path, auth in ((f"/api/materials/{entity_id}", False), (f"/api/for-admin/materials/{entity_id}", True)):
            response = request("GET", path, auth=auth)
            expect(response[0], 200, "variant card")
            cards.append(json_body(response))
        return cards

    def check_model():
        expected = {output: model.get(field) for field, (_, output) in fields.items()}
        cards = snapshots(material_id)
        search = public_query("search", {"search": model["name"]})
        equal(len(search["items"]), 1, "variant search count")
        for card in cards + search["items"]:
            equal(card["images"], expected, "variant projection")
            equal(card["article"], model["article"], "variant article")
        for guid in expected.values():
            if guid:
                file_check(guid)

    def update():
        expect(request("PUT", f"/api/for-admin/materials/{material_id}", model, auth=True)[0], 200, "variant update")
        check_model()

    try:
        model.update({field: new_image(field) for field in fields})
        material_id = create_material(model)
        check_model()
        mark("VAR-01", "Проверены три назначения изображений, поля ответов, MIME-типы и байты файлов")
        for field in fields:
            wrong_field = next(other for other in fields if other != field)
            wrong_guid = new_image(wrong_field)
            bad = dict(model, **{field: wrong_guid})
            before = snapshots(material_id)
            problem(request("PUT", f"/api/for-admin/materials/{material_id}", bad, auth=True), "wrong image type update")
            equal(snapshots(material_id), before, "failed update unchanged")
            bad.update(name=prefix + "-wrong-type", **{other: None for other in fields if other != field})
            problem(request("POST", "/api/for-admin/materials", bad, auth=True), "wrong image type create")
            equal(public_query("search", {"search": bad["name"]})["totalCount"], 0, "rejected create absent")
            file_check(wrong_guid)
        mark("VAR-02", "POST и PUT отклоняют изображения неверного назначения без изменения данных")
        update()
        model["article"] += "-updated"
        update()
        mark("VAR-03", "Повторное сохранение и изменение обычного поля сохраняют все изображения")
        for field in ("thumbnail240", "thumbnail480"):
            old = model[field]
            file_check(old)
            model[field] = new_image(field)
            update()
            problem(request("GET", "/api/materials/images/" + old), "replaced thumbnail")
        mark("VAR-04", "Замена отдельной миниатюры делает прежний GUID недоступным, включая кеш")
        other = material_model(prefix + "-variants-B", category_id, "raskroy")
        other.update({field: new_image(field) for field in fields})
        other_id = create_material(other)
        for field in ("thumbnail240", "thumbnail480"):
            before_a, before_b = snapshots(material_id), snapshots(other_id)
            bad = dict(other, **{field: model[field]})
            problem(request("PUT", f"/api/for-admin/materials/{other_id}", bad, auth=True), "foreign thumbnail update")
            equal(snapshots(material_id), before_a, "owner unchanged")
            equal(snapshots(other_id), before_b, "recipient unchanged")
            bad.update(name=prefix + "-foreign-variant", **{key: None for key in fields if key != field})
            problem(request("POST", "/api/for-admin/materials", bad, auth=True), "foreign thumbnail create")
            equal(public_query("search", {"search": bad["name"]})["totalCount"], 0, "foreign create absent")
            for guid in [model[field], *[other[key] for key in fields]]:
                file_check(guid)
        mark("VAR-06", "Миниатюры другого материала нельзя использовать повторно")
        new_original = new_image("image")
        before = snapshots(material_id)
        bad = dict(model, article="should-not-save", image=new_original, thumbnail480=str(uuid.uuid4()))
        problem(request("PUT", f"/api/for-admin/materials/{material_id}", bad, auth=True), "mixed invalid update")
        equal(snapshots(material_id), before, "atomic refusal")
        check_model()
        file_check(new_original)
        model["image"] = new_original
        update()
        mark("VAR-07", "Неуспешное смешанное обновление сохраняет данные; загруженный файл остаётся пригодным для привязки")
        for field in fields:
            old = model[field]
            file_check(old)
            model[field] = None
            update()
            problem(request("GET", "/api/materials/images/" + old), "removed image")
        mark("VAR-05", "Каждое изображение удаляется независимо от остальных")
        for guid in (other[field] for field in fields):
            file_check(guid)
        expect(delete_material(other_id)[0], 200, "delete all image variants")
        for field in fields:
            problem(request("GET", "/api/materials/images/" + other[field]), "deleted variant")
        for path in (f"/api/materials/{other_id}", f"/api/for-admin/materials/{other_id}"):
            problem(request("GET", path, auth=True), "deleted variant card")
        equal(public_query("search", {"search": other["name"]})["totalCount"], 0, "deleted variant search")
        mark("VAR-08", "Удаление материала делает все варианты изображений недоступными, включая кеш")
    finally:
        # Consume even uploads left by failed assertions before outer cleanup removes relations.
        for guid, (field, _) in uploaded.items():
            response = request("GET", "/api/materials/images/" + guid)
            if response[0] == 200:
                attached = any(
                    guid in snapshots(entity_id)[1]["images"].values()
                    for entity_id in created_materials)
                if not attached:
                    temporary = material_model(prefix + "-drain-" + guid, category_id, "raskroy")
                    temporary[field] = guid
                    temporary_id = create_material(temporary)
                    expect(delete_material(temporary_id)[0], 200, "unused upload cleanup")
        for entity_id in list(created_materials):
            card = json_body(request("GET", f"/api/for-admin/materials/{entity_id}", auth=True))
            if card and card.get("name", "").startswith(prefix + "-variants-"):
                expect(delete_material(entity_id)[0], 200, "variant fixture cleanup")
        for guid in uploaded:
            problem(request("GET", "/api/materials/images/" + guid), "final variant cleanup")


def run_thickness_scenarios(prefix, dimension_base, user_cookie, tester_cookie, manager_cookie, admin_cookie):
    path = "/api/for-admin/materials/thicknesses"
    value = dimension_base + 0.125
    model = {"name": prefix + "-thickness", "value": value}
    for auth_cookie, status in ((None, 401), (user_cookie, 403)):
        expect(request("POST", path, model, auth_cookie=auth_cookie)[0], status, "Доступ к созданию толщины")
    response = request("POST", path, model, auth_cookie=tester_cookie)
    expect(response[0], 200, "Создание толщины")
    entity_id = json_body(response)["id"]
    created_thicknesses.append(entity_id)
    expected = dict(model, id=entity_id)
    equal(json_body(request("GET", f"{path}/{entity_id}", auth=True)), expected, "Карточка толщины")
    items = json_body(request("GET", path, auth=True))["items"]
    equal(next(x for x in items if x["id"] == entity_id),
          dict(expected, materialsAnyCount=0, materialsNotAnyCount=0), "Список толщин")
    equal([x["value"] for x in items], sorted(x["value"] for x in items), "Порядок толщин")
    mark("THICK-01", "Создание, административные список и карточка, сортировка по значению")
    for auth_cookie, status in ((None, 401), (user_cookie, 403)):
        for method in ("PUT", "DELETE"):
            expect(request(method, f"{path}/{entity_id}", model if method == "PUT" else None,
                           auth_cookie=auth_cookie)[0], status, "Права на изменение толщины")
    for auth_cookie in (tester_cookie, manager_cookie, admin_cookie):
        expect(request("PUT", f"{path}/{entity_id}", model, auth_cookie=auth_cookie)[0], 200, "Сохранение без изменений")
    mark("THICK-02", "Проверены роли для создания, изменения и удаления; разрешённое сохранение без изменений")
    for invalid in ({"name": "", "value": value + 1}, {"name": " " * 5, "value": value + 1},
                    {"name": "x" * 101, "value": value + 1}, {"value": value + 1},
                    {"name": model["name"]}, {"name": model["name"], "value": 0},
                    {"name": model["name"], "value": -1}, {"name": model["name"], "value": "NaN"},
                    {"name": model["name"], "value": "Infinity"},
                    {"name": model["name"], "value": dimension_base + 0.1251},
                    {"name": model["name"], "value": dimension_base + 0.125001}):
        for method, url in (("POST", path), ("PUT", f"{path}/{entity_id}")):
            problem(request(method, url, invalid, auth=True), "Валидация толщины")
    problem(request("POST", path, model, auth=True), "Дубликат значения толщины")
    problem(request("PUT", f"{path}/{entity_id}", dict(model, value=18), auth=True), "Дубликат толщины при изменении")
    equal(json_body(request("GET", f"{path}/{entity_id}", auth=True)), expected, "Атомарность отказа")
    for invalid_id in (0, -1, 2147483647):
        for method in ("GET", "PUT", "DELETE"):
            problem(request(method, f"{path}/{invalid_id}", model if method == "PUT" else None,
                            auth=True), "Неизвестная толщина")
    mark("THICK-03", "Проверены обязательность, длина, положительность, конечность и неизвестные ID")
    for name in ("x" * 100, "  " + model["name"] + "-renamed  "):
        expect(request("PUT", f"{path}/{entity_id}", dict(model, name=name), auth=True)[0], 200, "Переименование толщины")
        equal(json_body(request("GET", f"{path}/{entity_id}", auth=True))["name"], name.strip(), "Имя толщины после изменения")
    expected["name"] = model["name"] + "-renamed"
    expected["value"] = value + 0.25
    expect(request("PUT", f"{path}/{entity_id}", {"name": expected["name"], "value": expected["value"]}, auth=True)[0], 200, "Изменение значения толщины")
    equal(json_body(request("GET", f"{path}/{entity_id}", auth=True)), expected, "Изменённая толщина")
    mark("THICK-04", "Имя длиной 100, Trim, изменение имени и числового значения")
    category = create_category(prefix + "-thickness-cat")
    maker = create_manufacturer(prefix + "-thickness-maker")
    size = create_sheet_size(prefix + "-thickness-size", dimension_base + 200, dimension_base + 201)
    material = material_model(prefix + "-thickness-material", category, "raskroy")
    material.update(manufacturerId=maker, sheetSizeId=size, thicknessId=entity_id)
    material_id = create_material(material)
    nested_expected = {"id": entity_id, "name": expected["name"]}
    problem(request("DELETE", f"{path}/{entity_id}", auth=True), "Удаление используемой толщины")
    for endpoint, auth in ((f"/api/materials/{material_id}", False), (f"/api/for-admin/materials/{material_id}", True)):
        body = json_body(request("GET", endpoint, auth=auth))
        equal(body["thickness"], nested_expected, "Вложенная толщина в карточке")
        assert "depth" not in body and "thicknessId" not in body
    for endpoint, auth in (("/api/materials", False), ("/api/for-admin/materials", True)):
        items = json_body(request("GET", endpoint, auth=auth))["items"]
        equal(next(x["thickness"] for x in items if x["id"] == material_id), nested_expected,
              "Вложенная толщина в полном списке")
    for endpoint, auth in (("/api/materials/search", False),):
        response = request("GET", endpoint + "?thicknessIds=" + str(entity_id), auth=auth)
        expect(response[0], 200, "Фильтр толщины")
        equal([(x["id"], x["thickness"]) for x in json_body(response)["items"]], [(material_id, nested_expected)], "Список по толщине")
        for bad in ("0", "-1", "2147483647", "abc", "&thicknessIds=".join([str(entity_id)] * 101)):
            problem(request("GET", endpoint + "?thicknessIds=" + bad, auth=auth), "Некорректный фильтр толщины")
    for bad in (0, -1, 2147483647):
        for method, url in (("POST", "/api/for-admin/materials"), ("PUT", f"/api/for-admin/materials/{material_id}")):
            problem(request(method, url, dict(material, thicknessId=bad), auth=True), "Толщина материала не найдена")
    mark("THICK-05", "Связь материала, защита удаления, вложенный DTO в пяти выдачах, фильтр поиска")
    search = public_query("search", {"search": material["name"]})
    equal((search["page"], search["pageSize"], search["totalCount"]), (1, 24, 1), "Пагинация по умолчанию")
    equal(public_query("search", {"search": material["name"], "thicknessIds": [entity_id, entity_id]})["totalCount"], 1, "Повтор толщины")
    equal(public_query("search", {"search": material["name"], "thicknessIds": [entity_id], "depths": [18]})["totalCount"], 0, "Пересечение depths и thicknessIds")
    public_query("search", {})
    public_query("filters", {})
    for endpoint in ("search", "filters"):
        problem(request("GET", "/api/materials/" + endpoint + "?search=" + "x" * 251), "Валидация без массивов")
        facets = public_query("filters", {"search": material["name"], "thicknessIds": [thickness_ids[18]]})
        actual = {x["id"]: x["count"] for x in facets["thicknesses"]}
        equal(actual, {entity_id: 1, thickness_ids[18]: 0}, "Собственный фильтр и нулевой выбранный вариант")
    mark("QUERY-01", "Отсутствующие массивы, запросы без параметров и пагинация по умолчанию")
    mark("THICK-06", "Повторы, пересечение с числовым фильтром depths, фасеты и нулевой выбранный вариант")
    expect(delete_material(material_id)[0], 200, "Очистка материала толщины")
    expect(request("DELETE", f"{path}/{entity_id}", auth=True)[0], 200, "Удаление свободной толщины")
    created_thicknesses.remove(entity_id)
    problem(request("GET", f"{path}/{entity_id}", auth=True), "Удалённая толщина")
    for entity, endpoint, tracked in ((category, "categories", created_categories), (maker, "manufacturers", created_manufacturers), (size, "sheet-sizes", created_sheet_sizes)):
        expect(request("DELETE", f"/api/for-admin/materials/{endpoint}/{entity}", auth=True)[0], 200, "Очистка справочника")
        tracked.remove(entity)
    equal(history(search=prefix + "-thickness")["totalCount"], 0, "Тестер не создаёт историю толщин")
    mark("THICK-07", "Свободная толщина удалена, повторное чтение отклонено, тестер не создаёт историю")



def run_decimal_scenarios(prefix, dimension_base, user_cookie, tester_cookie, manager_cookie, admin_cookie):
    path = "/api/for-admin/materials/thicknesses"
    thickness_id = None
    category = create_category(prefix + "-decimal-category")
    maker = create_manufacturer(prefix + "-decimal-maker")
    size = create_sheet_size(prefix + "-decimal-size", dimension_base + 300, dimension_base + 301)
    model = material_model(prefix + "-decimal-material", category, "raskroy")
    model.update(manufacturerId=maker, sheetSizeId=size)
    material_id = create_material(model)
    for value in (dimension_base, dimension_base + .1, dimension_base + .12, dimension_base + .123):
        body = {"name": prefix + "-decimal-thickness", "value": value}
        response = request("POST", path, body, auth=True)
        expect(response[0], 200, "Создание decimal толщины")
        thickness_id = json_body(response)["id"]
        created_thicknesses.append(thickness_id)
        expect(request("PUT", f"{path}/{thickness_id}", body, auth=True)[0], 200, "Сохранение decimal толщины")
        equal(json_body(request("GET", f"{path}/{thickness_id}", auth=True))["value"], value, "Точное значение толщины")
        measurement = value - dimension_base + 1
        measurement = round(measurement, 3)
        candidate = dict(model, kvM=measurement, perimetrM=measurement)
        temporary_id = create_material(dict(candidate, name=model["name"] + "-temporary"))
        equal(json_body(request("GET", f"/api/for-admin/materials/{temporary_id}", auth=True))["kvM"], measurement, "Точная площадь при создании")
        expect(delete_material(temporary_id)[0], 200, "Очистка decimal материала")
        expect(request("PUT", f"/api/for-admin/materials/{material_id}", candidate, auth=True)[0], 200, "Обновление decimal материала")
        card = json_body(request("GET", f"/api/for-admin/materials/{material_id}", auth=True))
        equal((card["kvM"], card["perimetrM"]), (measurement, measurement), "Точные измерения после обновления")
        if value != dimension_base + .123:
            expect(request("DELETE", f"{path}/{thickness_id}", auth=True)[0], 200, "Очистка толщины")
            created_thicknesses.remove(thickness_id)
    mark("DEC-01", "POST/PUT толщины, площади и периметра: 0–3 знака сохранены точно")
    for invalid in (1.1234, 1000000000000000):
        for method, url in (("POST", path), ("PUT", f"{path}/{thickness_id}")):
            problem(request(method, url, {"name": prefix + "-invalid", "value": invalid}, auth=True), "Точность толщины")
        for field in ("kvM", "perimetrM"):
            for method, url in (("POST", "/api/for-admin/materials"), ("PUT", f"/api/for-admin/materials/{material_id}")):
                problem(request(method, url, dict(model, **{field: invalid}), auth=True), "Точность измерения")
    mark("DEC-02", "POST/PUT отклоняют лишнюю дробную и целую цифру")
    raw = json.dumps(dict(model, kvM=1.123, perimetrM=1.123)).replace("1.123", "1.1230").encode()
    expect(request("PUT", f"/api/for-admin/materials/{material_id}", raw, auth=True, raw=True, headers={"Content-Type": "application/json"})[0], 200, "Завершающий ноль измерений")
    raw = ('{"name": "' + prefix + '-decimal-thickness", "value": ' + str(dimension_base) + '.1230}').encode()
    expect(request("PUT", f"{path}/{thickness_id}", raw, auth=True, raw=True, headers={"Content-Type": "application/json"})[0], 200, "Завершающий ноль толщины")
    mark("DEC-03", "JSON с завершающим нулём принят без потери значения")
    new_value = dimension_base + .124
    expect(request("PUT", f"{path}/{thickness_id}", {"name": prefix + "-decimal-thickness", "value": new_value}, auth=True)[0], 200, "Шаг 0.001")
    expect(request("PUT", f"/api/for-admin/materials/{material_id}", dict(model, thicknessId=thickness_id), auth=True)[0], 200, "Связь decimal толщины")
    equal(public_query("search", {"search": model["name"], "depths": [new_value]})["totalCount"], 1, "Поиск decimal")
    facets = public_query("filters", {"search": model["name"]})
    equal(next(x["value"] for x in facets["thicknesses"] if x["id"] == thickness_id), new_value, "Decimal фасета")
    mark("DEC-04", "Шаг 0.001 сохранён, поиск Depths и фасеты возвращают новое значение")
    expect(delete_material(material_id)[0], 200, "Очистка материала")
    expect(request("DELETE", f"{path}/{thickness_id}", auth=True)[0], 200, "Очистка толщины")
    created_thicknesses.remove(thickness_id)
    expect(request("DELETE", f"/api/for-admin/materials/categories/{category}", auth=True)[0], 200, "Очистка категории")
    created_categories.remove(category)
    for endpoint, entity_id, tracked in (("manufacturers", maker, created_manufacturers), ("sheet-sizes", size, created_sheet_sizes)):
        expect(request("DELETE", f"/api/for-admin/materials/{endpoint}/{entity_id}", auth=True)[0], 200, "Очистка справочника")
        tracked.remove(entity_id)


def run_material_type_scenarios(prefix, user_cookie):
    path = "/api/for-admin/materials/material-types"
    ids = []
    for suffix in ("a", "b", "empty"):
        response = request("POST", path, {"name": prefix + "-type-" + suffix}, auth=True)
        expect(response[0], 200, "Создание типа")
        ids.append(json_body(response)["id"])
        created_material_types.append(ids[-1])
    a, b, empty = ids
    renamed = prefix + "-type-renamed"
    expect(request("PUT", f"{path}/{a}", {"name": renamed}, auth=True)[0], 200, "Переименование типа")
    equal(json_body(request("GET", f"{path}/{a}", auth=True))["name"], renamed, "Название типа")
    items = json_body(request("GET", path, auth=True))["items"]
    equal([x["id"] for x in items[-3:]], ids, "Типы в конце списка")
    mark("MT-01", "Создание, чтение, переименование и порядок типов")
    for name in ("", "x" * 101):
        for method, url in (("POST", path), ("PUT", f"{path}/{a}")):
            problem(request(method, url, {"name": name}, auth=True), "Валидация типа")
    for method in ("GET", "PUT", "DELETE"):
        problem(request(method, path + "/2147483647", {"name": renamed} if method == "PUT" else None, auth=True), "Неизвестный тип")
    mark("MT-02", "Неверные названия и неизвестные ID отклонены")
    for direction, order in (("UP", [b, a, empty]), ("DOWN", ids)):
        expect(request("POST", path + "/change-order-col", {"id": b, "direction": direction}, auth=True)[0], 200, "Перестановка типов")
        equal([x["id"] for x in json_body(request("GET", path, auth=True))["items"][-3:]], order, "Порядок после перестановки")
    for entity, direction in ((items[0]["id"], "UP"), (empty, "DOWN")):
        problem(request("POST", path + "/change-order-col", {"id": entity, "direction": direction}, auth=True), "Граница порядка")
    mark("MT-03", "Перестановка UP/DOWN и обе границы")
    category = create_category(prefix + "-type-category")
    model = dict(material_model(prefix + "-typed", category, "raskroy"), materialTypeId=a)
    material_id = create_material(model)
    for value in (None, 0, 2147483647):
        invalid = dict(model, materialTypeId=value)
        if value is None:
            del invalid["materialTypeId"]
        for method, url in (("POST", "/api/for-admin/materials"), ("PUT", f"/api/for-admin/materials/{material_id}")):
            problem(request(method, url, invalid, auth=True), "Обязательный тип")
    expect(request("PUT", f"/api/for-admin/materials/{material_id}", dict(model, materialTypeId=b), auth=True)[0], 200, "Изменение типа материала")
    mark("MT-04", "Создание и изменение связи, обязательность и неизвестный ID")
    for endpoint, auth in ((f"/api/materials/{material_id}", False), (f"/api/for-admin/materials/{material_id}", True)):
        equal(json_body(request("GET", endpoint, auth=auth))["materialType"]["id"], b, "Тип в карточке")
    for endpoint, auth in (("/api/materials", False), ("/api/for-admin/materials", True), ("/api/materials/search?search=" + model["name"], False)):
        item = next(x for x in json_body(request("GET", endpoint, auth=auth))["items"] if x["id"] == material_id)
        equal(set(item["materialType"]), {"id", "name"}, "Контракт вложенного типа")
        equal(item["materialType"]["id"], b, "Тип в списке")
    mark("MT-08", "PropertyDto типа в пяти ответах, обязательность проверена HTTP")
    for selected, count in (([a], 0), ([b], 1), ([a, b], 1), ([b, b], 1)):
        equal(public_query("search", {"search": model["name"], "materialTypeIds": selected, "categoryIds": [category], "manufacturerIds": [model["manufacturerId"]]})["totalCount"], count, "Комбинированный поиск типов")
    mark("MT-06", "OR типов, повтор ID, AND категории и производителя")
    facets = public_query("filters", {"search": model["name"], "materialTypeIds": [empty]})
    equal({x["id"]: x["count"] for x in facets["materialTypes"]}, {b: 1, empty: 0}, "Фасета типов")
    mark("MT-07", "Собственный фильтр исключён, выбранный пустой тип сохранён")
    for auth_cookie, status in ((None, 401), (user_cookie, 403)):
        for method, url, body in (("GET", path, None), ("POST", path, {"name": renamed}), ("PUT", f"{path}/{a}", {"name": renamed}), ("DELETE", f"{path}/{a}", None), ("POST", path + "/change-order-col", {"id": b, "direction": "UP"})):
            expect(request(method, url, body, auth_cookie=auth_cookie)[0], status, "Авторизация типов")
    mark("MT-09", "Аноним и пользователь: 401/403")
    problem(request("DELETE", f"{path}/{b}", auth=True), "Удаление используемого типа")
    expect(delete_material(material_id)[0], 200, "Удаление материала")
    for entity in ids:
        expect(request("DELETE", f"{path}/{entity}", auth=True)[0], 200, "Удаление свободного типа")
        created_material_types.remove(entity)
        problem(request("GET", f"{path}/{entity}", auth=True), "Удалённый тип")
    mark("MT-05", "Используемый тип защищён, свободные удалены")
    expect(request("DELETE", f"/api/for-admin/materials/categories/{category}", auth=True)[0], 200, "Удаление категории")
    created_categories.remove(category)
    equal(history(search=prefix + "-type")["totalCount"], 0, "Нет истории тестера")
    mark("MT-10", "Тестовые типы и материал удалены, истории нет")


def run_visibility_scenarios(prefix, hidden_material_id, hidden_material_model,
                             category_id, category_model, category_material_id):
    hidden_material = dict(hidden_material_model, hideOnSite=True)
    expect(request("PUT", f"/api/for-admin/materials/{hidden_material_id}", hidden_material, auth=True)[0],
           200, "Скрытие материала")
    admin_card = json_body(request("GET", f"/api/for-admin/materials/{hidden_material_id}", auth=True))
    assert admin_card["hideOnSite"] is True and admin_card["hidePriceOnSite"] is False
    admin_item = next(x for x in admin_materials() if x["id"] == hidden_material_id)
    assert admin_item["hideOnSite"] is True and admin_item["hidePriceOnSite"] is False
    expect(request("GET", f"/api/materials/{hidden_material_id}")[0], 400, "Скрытая карточка материала")
    assert hidden_material_id not in {x["id"] for x in public_materials()}
    equal(public_query("search", {"search": hidden_material_model["name"]})["totalCount"], 0,
          "Скрытый материал в поиске")
    facets = public_query("filters", {"search": hidden_material_model["name"]})
    assert not any(facets[name] for name in ("categories", "manufacturers", "materialTypes", "thicknesses", "sheetSizes"))
    mark("VIS-01", "Скрытый материал доступен администратору и исключён из публичных списка, карточки, поиска и фасетов")

    hidden_price_material = dict(hidden_material_model, hidePriceOnSite=True)
    expect(request("PUT", f"/api/for-admin/materials/{hidden_material_id}", hidden_price_material, auth=True)[0],
           200, "Скрытие цены материала")
    admin_card = json_body(request("GET", f"/api/for-admin/materials/{hidden_material_id}", auth=True))
    assert admin_card["hideOnSite"] is False and admin_card["hidePriceOnSite"] is True
    admin_item = next(x for x in admin_materials() if x["id"] == hidden_material_id)
    assert admin_item["hideOnSite"] is False and admin_item["hidePriceOnSite"] is True
    public_card = json_body(request("GET", f"/api/materials/{hidden_material_id}"))
    assert "price" not in public_card and "hidePriceOnSite" not in public_card
    mark("VIS-02", "Признак скрытия цены сохраняется в административных ответах и не расширяет публичный контракт")
    expect(request("PUT", f"/api/for-admin/materials/{hidden_material_id}", hidden_material_model, auth=True)[0],
           200, "Восстановление видимости материала")

    hidden_category = dict(category_model, hideOnSite=True)
    expect(request("PUT", f"/api/for-admin/materials/categories/{category_id}", hidden_category, auth=True)[0],
           200, "Скрытие категории")
    category_card = json_body(request("GET", f"/api/for-admin/materials/categories/{category_id}", auth=True))
    assert category_card["hideOnSite"] is True
    category_item = next(x for x in categories() if x["id"] == category_id)
    assert category_item["hideOnSite"] is True
    expect(request("GET", f"/api/materials/{category_material_id}")[0], 400, "Материал скрытой категории")
    assert category_material_id not in {x["id"] for x in public_materials()}
    equal(public_query("search", {"search": prefix})["totalCount"], 2, "Материалы скрытой категории в поиске")
    problem(request("GET", "/api/materials/search?" + urllib.parse.urlencode({"categoryIds": category_id})),
            "Скрытая категория в поиске")
    problem(request("GET", "/api/materials/filters?" + urllib.parse.urlencode({"categoryIds": category_id})),
            "Скрытая категория в фасетах")
    mark("VIS-03", "Скрытая категория доступна администратору, скрывает материалы и отклоняется публичными фильтрами")

    visible_category = dict(category_model, hideOnSite=False)
    expect(request("PUT", f"/api/for-admin/materials/categories/{category_id}", visible_category, auth=True)[0],
           200, "Восстановление видимости категории")
    expect(request("GET", f"/api/materials/{category_material_id}")[0], 200,
           "Карточка материала после восстановления категории")
    mark("VIS-04", "Видимость материала и категории восстановлена перед остальной регрессией")


def main():
    global cookie, history_run_id, primary_sheet_size_id, primary_manufacturer_id, primary_material_type_id
    prefix = "codex-http-" + time.strftime("%Y%m%d-%H%M%S") + "-" + uuid.uuid4().hex[:6]
    dimension_base = 10000 + int(uuid.uuid4().hex[:4], 16)
    png = b"\x89PNG\r\n\x1a\n" + b"catalog-png-test"
    jpeg = b"\xff\xd8\xff\xe0" + b"catalog-jpeg-test" + b"\xff\xd9"
    webp = b"RIFF" + (20).to_bytes(4, "little") + b"WEBP" + b"catalog-webp-test"

    start_server()
    openapi_response = request("GET", "/swagger/v1/swagger.json")
    expect(openapi_response[0], 200, "OpenAPI document")
    openapi_document = json_body(openapi_response)
    for endpoint in ("/api/materials", "/api/for-admin/materials"):
        equal(openapi_document["paths"][endpoint]["get"].get("parameters", []), [],
              "Полный список не имеет query-параметров в OpenAPI")
    schemas = openapi_document["components"]["schemas"]
    thickness_schema = schemas["PropertyDto"]
    equal(set(thickness_schema["properties"]), {"id", "name"}, "Поля DTO толщины")
    equal(set(thickness_schema["required"]), {"id", "name"}, "Обязательные поля DTO толщины")
    for schema_name in ("GetAllMaterialsQueryItemResult", "GetMaterialsQueryForAdminItemResult",
                        "GetMaterialQueryResult", "GetMaterialForAdminQueryResult", "PublicMaterialListItemDto"):
        properties = schemas[schema_name]["properties"]
        assert "depth" not in properties and "thicknessId" not in properties, schema_name
        assert "PropertyDto" in json.dumps(properties["thickness"]), schema_name
    mark("DOC-03", "В пяти ответах материала вложенный PropertyDto с обязательными id/name")
    for endpoint, count in (("search", 12), ("filters", 9)):
        parameters = openapi_document["paths"]["/api/materials/" + endpoint]["get"]["parameters"]
        equal(len(parameters), count, "Количество query-параметров")
        parameter_names = {parameter["name"][0].lower() + parameter["name"][1:] for parameter in parameters}
        equal(parameter_names & {"categoryIds", "manufacturerIds", "materialTypeIds", "sheetSizeIds", "thicknessIds", "depths", "calculators"},
              {"categoryIds", "manufacturerIds", "materialTypeIds", "sheetSizeIds", "thicknessIds", "depths", "calculators"}, "Имена параметров множественного выбора")
        assert not parameter_names & {"categoryId", "manufacturerId", "sheetSizeId", "thicknessId", "depth", "calculator"}, "Старые имена массивов ID в OpenAPI"
        for parameter in parameters:
            assert not parameter.get("required", False), parameter["name"]
            assert re.search("[А-Яа-я]", parameter.get("description", "")), parameter["name"]
    mark("QUERY-02", "Все query-параметры необязательны и имеют русские описания в OpenAPI")
    sheet_size_schema = openapi_document["components"]["schemas"]["GetMaterialSheetSizeQueryResult"]
    required_sheet_size_properties = {"id", "name", "height", "width", "showInFilters", "orderByCol"}
    if not required_sheet_size_properties.issubset(sheet_size_schema.get("properties", {})):
        raise AssertionError("GetMaterialSheetSizeQueryResult OpenAPI properties are incomplete")
    if not required_sheet_size_properties.issubset(set(sheet_size_schema.get("required", []))):
        raise AssertionError("GetMaterialSheetSizeQueryResult OpenAPI required properties are incomplete")
    mark("DOC-01", "Схема GetMaterialSheetSizeQueryResult в OpenAPI содержит все свойства ответа и обязательные поля")
    if "/api/for-admin/catalog/history/test-runs/{runId}" in openapi_document["paths"]:
        raise AssertionError("Метод очистки тестовой истории не должен отображаться в OpenAPI")
    mark("DOC-02", "Метод очистки тестовой истории скрыт из OpenAPI")
    if "/api/materials/categories" in openapi_document["paths"]:
        raise AssertionError("Публичный метод категорий не должен отображаться в OpenAPI")
    mark("DOC-04", "Публичные методы категорий отсутствуют в OpenAPI")

    expected_tag_names = [
        "Materials",
        "MaterialsForAdmin",
        "MaterialsForCalculate",
        "MaterialCategoriesForAdmin",
        "MaterialManufacturersForAdmin",
        "MaterialSheetSizesForAdmin",
        "MaterialThicknessesForAdmin",
        "MaterialTypes",
        "MaterialTypesForAdmin",
        "CatalogHistoryForAdmin",
    ]
    equal([tag["name"] for tag in openapi_document.get("tags", [])], expected_tag_names,
          "Порядок групп контроллеров в Scalar")
    mark("DOC-06", "Порядок групп OpenAPI для Scalar соответствует согласованному OpenApiTagOrder")

    credentials = load_test_credentials()
    user_cookie = signin(credentials["user"], credentials["password"], "user")
    tester_cookie = signin(credentials["tester"], credentials["password"], "tester")
    manager_cookie = signin(credentials["manager"], credentials["password"], "manager")
    admin_cookie = signin(credentials["administrator"], credentials["password"], "administrator")
    cookie = tester_cookie
    role_probes = [
        ("GET", "/api/for-admin/materials", 200),
        ("GET", "/api/for-admin/catalog/history", 200),
        ("DELETE", "/api/for-admin/materials/categories/2147483647", 400),
        ("DELETE", "/api/for-admin/materials/sheet-sizes/2147483647", 400),
        ("DELETE", "/api/for-admin/materials/manufacturers/2147483647", 400),
    ]
    for method, path, authorized_status in role_probes:
        expect(request(method, path, auth_cookie=user_cookie)[0], 403, f"ordinary user {method} {path}")
        expect(request(method, path, auth_cookie=tester_cookie)[0], authorized_status, f"tester {method} {path}")
        expect(request(method, path, auth_cookie=admin_cookie)[0], authorized_status, f"administrator {method} {path}")
    for resource in ("categories", "sheet-sizes", "manufacturers", "thicknesses"):
        admin_path = "/api/for-admin/materials/" + resource
        for endpoint in (admin_path, admin_path + "/1"):
            expect(request("GET", endpoint)[0], 401, "Административное чтение без cookie")
            expect(request("GET", endpoint, auth_cookie=user_cookie)[0], 403, "Административное чтение обычным пользователем")
        for role_cookie in (tester_cookie, manager_cookie, admin_cookie):
            response = request("GET", admin_path, auth_cookie=role_cookie)
            expect(response[0], 200, "Административный список с разрешённой ролью")
            items = json_body(response)["items"]
            if items:
                expect(request("GET", admin_path + "/" + str(items[0]["id"]), auth_cookie=role_cookie)[0],
                       200, "Административная карточка с разрешённой ролью")
    mark("AUTH-07A", "Административное чтение справочников требует разрешённой роли")
    expect(request("GET", "/api/for-admin/materials", auth_cookie=manager_cookie)[0], 200, "manager")
    expect(request("GET", "/api/for-admin/catalog/history")[0], 401, "anonymous history")
    expect(request("GET", "/api/for-admin/catalog/history", auth_cookie=user_cookie)[0], 403, "ordinary user history")
    expect(request("GET", "/api/for-admin/catalog/history?pageSize=201", auth_cookie=manager_cookie)[0], 400, "history page size validation")
    cleanup_probe_path = f"/api/for-admin/catalog/history/test-runs/{int(uuid.uuid4().hex[:7], 16) + 1}"
    expect(request("DELETE", cleanup_probe_path)[0], 401, "anonymous history cleanup")
    expect(request("DELETE", cleanup_probe_path, auth_cookie=user_cookie)[0], 403, "ordinary user history cleanup")
    expect(request("DELETE", cleanup_probe_path, auth_cookie=manager_cookie)[0], 403, "manager history cleanup")
    expect(request("DELETE", cleanup_probe_path, auth_cookie=admin_cookie)[0], 403, "administrator history cleanup")
    expect(request("DELETE", "/api/for-admin/catalog/history/test-runs/not-an-int",
                   auth_cookie=tester_cookie)[0], 404, "history cleanup run id route constraint")
    tester_cleanup_probe = request("DELETE", cleanup_probe_path, auth_cookie=tester_cookie)
    expect(tester_cleanup_probe[0], 200, "tester history cleanup")
    if json_body(tester_cleanup_probe)["deletedCount"] != 0:
        raise AssertionError("history cleanup probe deleted unrelated events")
    mark("PREP-01—PREP-03", "Получены cookie пользователя, тестера, менеджера и администратора; подготовлен анонимный клиент")
    mark("AUTH-08—AUTH-12", "Проверены доступ к закрытым методам и разрешение очистки истории только тестеру")

    material_types_response = request("GET", "/api/for-admin/materials/material-types", auth=True)
    expect(material_types_response[0], 200, "Получение типов материалов")
    material_types = json_body(material_types_response)["items"]
    if not material_types:
        raise AssertionError("Для регрессионного прогона требуется хотя бы один тип материала")
    primary_material_type_id = material_types[0]["id"]

    existing_thicknesses = json_body(request("GET", "/api/for-admin/materials/thicknesses", auth=True))["items"]
    for value in (16, 18):
        existing = next((x for x in existing_thicknesses if x["value"] == value), None)
        if existing:
            thickness_ids[value] = existing["id"]
        else:
            response = request("POST", "/api/for-admin/materials/thicknesses", {"name": prefix + f"-{value}", "value": value}, auth=True)
            expect(response[0], 200, "Создание толщины для тестовых материалов")
            thickness_ids[value] = json_body(response)["id"]
            created_thicknesses.append(thickness_ids[value])
    run_thickness_scenarios(prefix, dimension_base, user_cookie, tester_cookie, manager_cookie, admin_cookie)
    run_decimal_scenarios(prefix, dimension_base, user_cookie, tester_cookie, manager_cookie, admin_cookie)

    tester_probe_name = prefix + "-tester-no-history"
    tester_create = request("POST", "/api/for-admin/materials/manufacturers", {"name": tester_probe_name}, auth_cookie=tester_cookie)
    expect(tester_create[0], 200, "tester create manufacturer without history")
    tester_probe_id = json_body(tester_create)["id"]
    created_manufacturers.append(tester_probe_id)
    expect(request("PUT", f"/api/for-admin/materials/manufacturers/{tester_probe_id}", {
        "name": tester_probe_name + "-updated"
    }, auth_cookie=tester_cookie)[0], 200, "tester update manufacturer without history")
    expect(request("DELETE", f"/api/for-admin/materials/manufacturers/{tester_probe_id}", auth_cookie=tester_cookie)[0], 200,
           "tester delete manufacturer without history")
    created_manufacturers.remove(tester_probe_id)
    if history(search=tester_probe_name)["totalCount"] != 0:
        raise AssertionError("tester operations created catalog history events")
    mark("HIST-13", "Создание, изменение и удаление тестером выполнены без событий истории каталога")

    primary_sheet_size_id = create_sheet_size(prefix + "-size-a", dimension_base, dimension_base + 1)
    secondary_sheet_size_id = create_sheet_size(prefix + "-size-b", dimension_base + 2, dimension_base + 3)
    expect(request("GET", "/api/for-admin/materials/sheet-sizes", auth=True)[0], 200, "Список размеров для тестера")
    expect(request("GET", f"/api/for-admin/materials/sheet-sizes/{primary_sheet_size_id}", auth=True)[0], 200, "Карточка размера для тестера")
    expect(request("POST", "/api/for-admin/materials/sheet-sizes", {"name": prefix + "-other", "height": dimension_base, "width": dimension_base + 1}, auth=True)[0], 400, "duplicate material sheet size")
    expect(request("POST", "/api/for-admin/materials/sheet-sizes", {"name": prefix + "-size-a", "height": dimension_base + 4, "width": dimension_base + 5}, auth=True)[0], 400, "duplicate material sheet size name")
    temporary_sheet_size_id = create_sheet_size(prefix + "-size-temp", dimension_base + 6, dimension_base + 7)
    expect(request("PUT", f"/api/for-admin/materials/sheet-sizes/{temporary_sheet_size_id}", {"name": prefix + "-size-updated", "height": dimension_base + 8, "width": dimension_base + 9}, auth=True)[0], 200, "update material sheet size")
    expect(request("PUT", f"/api/for-admin/materials/sheet-sizes/{temporary_sheet_size_id}", {"name": prefix + "-size-updated", "height": dimension_base + 8, "width": dimension_base + 9}, auth=True)[0], 200, "no-op material sheet size update")
    expect(request("DELETE", f"/api/for-admin/materials/sheet-sizes/{temporary_sheet_size_id}", auth=True)[0], 200, "delete free material sheet size")
    created_sheet_sizes.remove(temporary_sheet_size_id)
    mark("SIZE-01—SIZE-04", "Проверены создание, список, карточка, защита от дублей, изменение и удаление свободного размера")

    own_sizes = [x for x in sheet_sizes() if x["id"] in (primary_sheet_size_id, secondary_sheet_size_id)]
    if [x["id"] for x in own_sizes] != [primary_sheet_size_id, secondary_sheet_size_id]:
        raise AssertionError("created material sheet size order is incorrect")
    expect(request("POST", "/api/for-admin/materials/sheet-sizes/change-order-col", {
        "id": secondary_sheet_size_id, "direction": "UP"
    }, auth=True)[0], 200, "move material sheet size up")
    own_sizes = [x for x in sheet_sizes() if x["id"] in (primary_sheet_size_id, secondary_sheet_size_id)]
    if [x["id"] for x in own_sizes] != [secondary_sheet_size_id, primary_sheet_size_id]:
        raise AssertionError("material sheet size was not moved up")
    expect(request("POST", "/api/for-admin/materials/sheet-sizes/change-order-col", {
        "id": secondary_sheet_size_id, "direction": "DOWN"
    }, auth=True)[0], 200, "move material sheet size down")
    own_sizes = [x for x in sheet_sizes() if x["id"] in (primary_sheet_size_id, secondary_sheet_size_id)]
    if [x["id"] for x in own_sizes] != [primary_sheet_size_id, secondary_sheet_size_id]:
        raise AssertionError("material sheet size was not moved down")
    mark("SIZE-08—SIZE-09", "Размер материала перемещён вверх и вниз")

    primary_manufacturer_id = create_manufacturer(prefix + "-manufacturer-a")
    run_material_type_scenarios(prefix, user_cookie)
    secondary_manufacturer_id = create_manufacturer(prefix + "-manufacturer-b")
    expect(request("GET", f"/api/for-admin/materials/manufacturers/{primary_manufacturer_id}", auth=True)[0], 200, "Карточка производителя для тестера")
    temporary_manufacturer_id = create_manufacturer(prefix + "-manufacturer-temp")
    expect(request("PUT", f"/api/for-admin/materials/manufacturers/{temporary_manufacturer_id}", {"name": prefix + "-manufacturer-updated"}, auth=True)[0], 200, "update manufacturer")
    expect(request("PUT", f"/api/for-admin/materials/manufacturers/{temporary_manufacturer_id}", {"name": prefix + "-manufacturer-updated"}, auth=True)[0], 200, "no-op manufacturer update")
    expect(request("DELETE", f"/api/for-admin/materials/manufacturers/{temporary_manufacturer_id}", auth=True)[0], 200, "delete free manufacturer")
    created_manufacturers.remove(temporary_manufacturer_id)
    expect(request("POST", "/api/for-admin/materials/manufacturers/change-order-col", {"id": secondary_manufacturer_id, "direction": "UP"}, auth=True)[0], 200, "manufacturer up")
    expect(request("POST", "/api/for-admin/materials/manufacturers/change-order-col", {"id": secondary_manufacturer_id, "direction": "DOWN"}, auth=True)[0], 200, "manufacturer down")
    own_manufacturers = [x["id"] for x in manufacturers() if x["id"] in (primary_manufacturer_id, secondary_manufacturer_id)]
    if own_manufacturers != [primary_manufacturer_id, secondary_manufacturer_id]:
        raise AssertionError("manufacturer sorting incorrect")
    all_manufacturers = manufacturers()
    expect(request("POST", "/api/for-admin/materials/manufacturers/change-order-col", {"id": all_manufacturers[0]["id"], "direction": "UP"}, auth=True)[0], 400, "manufacturer upper boundary")
    expect(request("POST", "/api/for-admin/materials/manufacturers/change-order-col", {"id": all_manufacturers[-1]["id"], "direction": "DOWN"}, auth=True)[0], 400, "manufacturer lower boundary")
    mark("MFR-01—MFR-06", "Проверены операции с производителями, сортировка и её границы")

    expect(request("GET", "/api/materials")[0], 200, "public materials")
    expect(request("GET", "/api/materials/categories")[0], 404, "removed public categories")
    expect(request("GET", "/api/for-admin/materials")[0], 401, "anonymous admin list")
    anonymous_mutations = [
        ("POST", "/api/for-admin/materials/categories", {"name": "x", "externalLink": "x"}),
        ("PUT", "/api/for-admin/materials/categories/1", {"name": "x", "externalLink": "x"}),
        ("DELETE", "/api/for-admin/materials/categories/1", None),
        ("POST", "/api/for-admin/materials/categories/change-order-col", {"categoryId": 1, "direction": "UP"}),
        ("POST", "/api/for-admin/materials/sheet-sizes", {"name": "x", "height": 1, "width": 1}),
        ("POST", "/api/for-admin/materials/sheet-sizes/change-order-col", {"id": 1, "direction": "UP"}),
        ("POST", "/api/for-admin/materials/manufacturers", {"name": "x"}),
        ("PUT", "/api/for-admin/materials/manufacturers/1", {"name": "x"}),
        ("DELETE", "/api/for-admin/materials/manufacturers/1", None),
        ("POST", "/api/for-admin/materials/manufacturers/change-order-col", {"id": 1, "direction": "UP"}),
    ]
    for method, path, body in anonymous_mutations:
        expect(request(method, path, body)[0], 401, f"anonymous {method} {path}")

    mark("MFR-10", "Анонимные изменения производителя возвращают 401")

    cat1, cat2, cat3 = [create_category(prefix + suffix) for suffix in ("-cat-a", "-cat-b", "-cat-c")]
    listed = categories()
    own = [x for x in listed if x["id"] in (cat1, cat2, cat3)]
    if [x["id"] for x in own] != [cat1, cat2, cat3]:
        raise AssertionError("created category order is incorrect")
    for category_id in (cat1, cat2, cat3):
        response = request("GET", f"/api/for-admin/materials/categories/{category_id}", auth=True)
        expect(response[0], 200, "admin category card")
    response = request("PUT", f"/api/for-admin/materials/categories/{cat2}", {
        "name": prefix + "-cat-b-updated", "externalLink": "https://example.test/updated", "hideOnSite": False
    }, auth=True)
    expect(response[0], 200, "update category")
    expect(request("PUT", f"/api/for-admin/materials/categories/{cat2}", {
        "name": prefix + "-cat-b-updated", "externalLink": "https://example.test/updated", "hideOnSite": False
    }, auth=True)[0], 200, "no-op category update")
    updated = json_body(request("GET", f"/api/for-admin/materials/categories/{cat2}", auth=True))
    if updated["name"] != prefix + "-cat-b-updated":
        raise AssertionError("category update not visible")
    expect(request("POST", "/api/for-admin/materials/categories/change-order-col", {"categoryId": cat2, "direction": "UP"}, auth=True)[0], 200, "category up")
    own_ids = [x["id"] for x in categories() if x["id"] in (cat1, cat2, cat3)]
    if own_ids != [cat2, cat1, cat3]:
        raise AssertionError("category UP sorting incorrect")
    expect(request("POST", "/api/for-admin/materials/categories/change-order-col", {"categoryId": cat2, "direction": "DOWN"}, auth=True)[0], 200, "category down")
    all_categories = categories()
    expect(request("POST", "/api/for-admin/materials/categories/change-order-col", {"categoryId": all_categories[0]["id"], "direction": "UP"}, auth=True)[0], 400, "category upper boundary")
    expect(request("POST", "/api/for-admin/materials/categories/change-order-col", {"categoryId": all_categories[-1]["id"], "direction": "DOWN"}, auth=True)[0], 400, "category lower boundary")
    mark("CAT-01—CAT-08", "Проверены создание, список, карточки, изменение, сортировка и обе её границы")

    payload, headers = multipart(
        "file", "anon.png", png, "image/png", {"imageType": "Original"})
    expect(request("POST", "/api/for-admin/materials/images", payload, headers=headers, raw=True)[0], 401, "anonymous image upload")
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
    mark("IMG-01—IMG-04", "Проверены авторизация, три формата, валидация и получение файлов до привязки")

    restart_server()
    response = request("GET", "/api/materials/images/" + images[0][0])
    expect(response[0], 200, "PNG after restart")
    if response[2] != png:
        raise AssertionError("PNG changed after restart")
    mark("IMG-05", "Загруженный PNG доступен после перезапуска")

    m1_model = material_model(prefix + "-mat-raskroy", cat1, "raskroy", images[0][0])
    m2_model = material_model(prefix + "-mat-pvh", cat1, "pvhFacades")
    m3_model = material_model(prefix + "-mat-emal", cat2, "emalFacades")
    m1, m2, m3 = [create_material(x) for x in (m1_model, m2_model, m3_model)]
    invalid_manufacturer_model = material_model(prefix + "-invalid-manufacturer", cat1, "raskroy")
    invalid_manufacturer_model["manufacturerId"] = 2147483647
    expect(request("POST", "/api/for-admin/materials", invalid_manufacturer_model, auth=True)[0], 400, "invalid manufacturer id")
    zero_manufacturer_model = material_model(prefix + "-zero-manufacturer", cat1, "raskroy")
    zero_manufacturer_model["manufacturerId"] = 0
    expect(request("POST", "/api/for-admin/materials", zero_manufacturer_model, auth=True)[0], 400, "zero manufacturer id")
    expect(request("GET", f"/api/materials/{m1}")[0], 200, "public material card")
    expect(request("GET", f"/api/for-admin/materials/{m1}", auth=True)[0], 200, "admin material card")
    material_card = json_body(request("GET", f"/api/materials/{m1}"))
    if material_card["sheetSize"]["id"] != primary_sheet_size_id:
        raise AssertionError("material sheet size absent from material card")
    if material_card["manufacturer"]["id"] != primary_manufacturer_id:
        raise AssertionError("manufacturer absent from material card")
    if "orderByCol" in material_card["sheetSize"] or "orderByCol" in material_card["category"] or "orderByCol" in material_card["manufacturer"]:
        raise AssertionError("nested material relations contain sorting fields")
    expect(request("DELETE", f"/api/for-admin/materials/sheet-sizes/{primary_sheet_size_id}", auth=True)[0], 400, "delete used material sheet size")
    expect(request("DELETE", f"/api/for-admin/materials/manufacturers/{primary_manufacturer_id}", auth=True)[0], 400, "delete used manufacturer")
    mark("MFR-07—MFR-08", "Связь с материалом отображается; удаление используемого производителя отклонено")
    mark("SIZE-05—SIZE-06", "Связь с материалом отображается; удаление используемого размера отклонено")
    expect(request("GET", f"/api/for-admin/materials/categories/{cat1}", auth=True)[0], 200, "admin category card after material")
    png_response = request("GET", "/api/materials/images/" + images[0][0])
    expect(png_response[0], 200, "bound PNG")
    if png_response[2] != png:
        raise AssertionError("bound PNG mismatch")
    mark("AUTH-01—AUTH-07", "Проверены публичные материалы и изображения, удаление публичных категорий и анонимные обращения к закрытым методам")
    mark("IMG-06—IMG-07", "После перезапуска создан материал с PNG; проверены карточка и изображение")

    admin_ids = {x["id"] for x in admin_materials()}
    if not {m1, m2, m3}.issubset(admin_ids):
        raise AssertionError("created materials absent from admin list")
    unfiltered_ids = {x["id"] for x in public_materials()}
    if not {m1, m2, m3}.issubset(unfiltered_ids):
        raise AssertionError("unfiltered public list omitted test materials")

    run_visibility_scenarios(
        prefix,
        m2,
        m2_model,
        cat2,
        {
            "name": prefix + "-cat-b-updated",
            "externalLink": "https://example.test/updated"
        },
        m3)

    for calculator, expected_id in (("Raskroy", m1), ("PvhFacades", m2), ("EmalFacades", m3)):
        ids = {x["id"] for x in public_query("search", {"search": prefix, "calculators": [calculator], "pageSize": 96})["items"]}
        if expected_id not in ids:
            raise AssertionError(f"filter {calculator} omitted expected material")
        wrong_test_ids = ({m1, m2, m3} - {expected_id}) & ids
        if wrong_test_ids:
            raise AssertionError(f"filter {calculator} returned inapplicable test material")

    combined_ids = {x["id"] for x in public_query("search", {"search": prefix, "calculators": ["Raskroy", "PvhFacades"], "pageSize": 96})["items"]}
    if not {m1, m2}.issubset(combined_ids) or m3 in combined_ids:
        raise AssertionError("combined calculator filter returned incorrect test materials")
    expect(request("GET", "/api/materials/search?calculators=Unknown")[0], 400, "invalid calculator")

    all_rows = [x for x in public_materials() if prefix in x["name"]]
    for stock in (True, False):
        rows = public_query("search", {"search": prefix, "inStock": str(stock).lower(), "pageSize": 96})["items"]
        equal({x["id"] for x in rows}, {x["id"] for x in all_rows if (x["count"] > 0) == stock},
              "Точное множество наличия в поиске")
    for endpoint, auth in (("/api/materials", False), ("/api/for-admin/materials", True)):
        baseline = json_body(request("GET", endpoint, auth=auth))
        response = request("GET", endpoint + "?calculators=Unknown&inStock=invalid&thicknessIds=-1", auth=auth)
        expect(response[0], 200, "Полный список не привязывает параметры фильтрации")
        equal(json_body(response), baseline, "Query-параметры не меняют полный список")
    mark("MAT-03C", "Наличие фильтруется в поиске; публичный и административный списки не принимают фильтры")

    m3_updated = copy.deepcopy(m3_model)
    m3_updated.update({
        "categoryId": cat3, "name": prefix + "-mat-emal-updated", "article": "UPDATED",
        "count": 7, "sheetSizeId": secondary_sheet_size_id, "thicknessId": thickness_ids[16], "kvM": 2.977,
        "perimetrM": 7.32, "price": 456.78, "countTypeEnum": "SHT",
        "applicableToCutting": True, "applicableToEnamelFacades": False,
        "commentOnMaterialIsRequired": True, "allowSecondItemInOrder": False
    })
    expect(request("PUT", f"/api/for-admin/materials/{m3}", m3_updated, auth=True)[0], 200, "update material")
    expect(request("PUT", f"/api/for-admin/materials/{m3}", m3_updated, auth=True)[0], 200, "no-op material update")
    public_card = json_body(request("GET", f"/api/materials/{m3}"))
    admin_card = json_body(request("GET", f"/api/for-admin/materials/{m3}", auth=True))
    if public_card["category"]["id"] != cat3 or admin_card["price"] != 456.78 or admin_card["countTypeEnum"] != "SHT":
        raise AssertionError("updated material fields not visible")

    expect(request("POST", "/api/for-admin/materials/change-order-col", {"id": m2, "direction": "UP"}, auth=True)[0], 200, "material up")
    cat1_order = [x["id"] for x in admin_materials() if x["category"]["id"] == cat1]
    if cat1_order != [m2, m1]:
        raise AssertionError("material UP sorting incorrect")
    cat3_before = [x["id"] for x in admin_materials() if x["category"]["id"] == cat3]
    expect(request("POST", "/api/for-admin/materials/change-order-col", {"id": m2, "direction": "DOWN"}, auth=True)[0], 200, "material down")
    if [x["id"] for x in admin_materials() if x["category"]["id"] == cat3] != cat3_before:
        raise AssertionError("sorting affected another category")
    cat1_rows = [x for x in admin_materials() if x["category"]["id"] == cat1]
    expect(request("POST", "/api/for-admin/materials/change-order-col", {"id": cat1_rows[0]["id"], "direction": "UP"}, auth=True)[0], 400, "material upper boundary")
    expect(request("POST", "/api/for-admin/materials/change-order-col", {"id": cat1_rows[-1]["id"], "direction": "DOWN"}, auth=True)[0], 400, "material lower boundary")
    expect(request("DELETE", f"/api/for-admin/materials/categories/{cat1}", auth=True)[0], 400, "delete used category")
    mark("CAT-09", "Удаление используемой категории отклонено; связанные сущности сохранены")
    mark("MAT-01—MAT-08", "Проверены создание, списки, карточки, фильтры, изменение, сортировка, её изоляция и границы")

    foreign_create_model = material_model(prefix + "-foreign-image-create", cat2, "raskroy", images[0][0])
    expect(request("POST", "/api/for-admin/materials", foreign_create_model, auth=True)[0], 400, "create with another material image")
    foreign_update_model = copy.deepcopy(m2_model)
    foreign_update_model["image"] = images[0][0]
    expect(request("PUT", f"/api/for-admin/materials/{m2}", foreign_update_model, auth=True)[0], 400, "update with another material image")
    if json_body(request("GET", f"/api/materials/{m2}"))["images"]["original"] is not None:
        raise AssertionError("foreign image was attached to another material")
    mark("VAL-11—VAL-12", "Создание и изменение отклонены при передаче изображения, привязанного к другому материалу")

    jpeg_model = copy.deepcopy(m1_model)
    jpeg_model["image"] = images[1][0]
    expect(request("PUT", f"/api/for-admin/materials/{m1}", jpeg_model, auth=True)[0], 200, "replace PNG with JPEG")
    if json_body(request("GET", f"/api/materials/{m1}"))["images"]["original"] != images[1][0]:
        raise AssertionError("JPEG GUID absent from material card")
    expect(request("GET", "/api/materials/images/" + images[0][0])[0], 400, "old PNG invalidated")
    for _ in range(2):
        response = request("GET", "/api/materials/images/" + images[1][0])
        expect(response[0], 200, "repeat JPEG GET")
        if response[2] != jpeg or response[1].get("Content-Type") != "image/jpeg":
            raise AssertionError("JPEG response mismatch")
    image_path = "/api/materials/images/" + images[1][0]
    etag = response[1].get("ETag")
    assert etag and images[1][0].lower() in etag.lower(), "ETag не соответствует GUID"
    cache_control = response[1].get("Cache-Control", "")
    assert "public" in cache_control and "max-age=31536000" in cache_control, "Некорректный Cache-Control"
    mark("IMG-09A", "ETag соответствует GUID, публичный кеш разрешён на год")
    conditional = request("GET", image_path, headers={"If-None-Match": etag})
    expect(conditional[0], 304, "Неизменившийся файл")
    equal(conditional[2], b"", "Тело ответа 304")
    mark("IMG-09B", "Совпадающий ETag возвращает 304 без тела")
    conditional = request("GET", image_path, headers={"If-None-Match": '"' + str(uuid.uuid4()) + '"'})
    expect(conditional[0], 200, "Несовпадающий ETag")
    equal((conditional[2], conditional[1].get("ETag"), conditional[1].get("Content-Type")),
          (jpeg, etag, "image/jpeg"), "Файл после несовпадающего ETag")
    mark("IMG-09C", "Несовпадающий ETag возвращает исходные байты и заголовки")
    time.sleep(1)
    expect(request("GET", "/api/materials/images/" + images[1][0])[0], 200, "later JPEG GET")
    mark("IMG-08—IMG-10", "Замена на JPEG сделала PNG недоступным; повторное и отложенное получение прошли")

    webp_model = copy.deepcopy(m1_model)
    webp_model["image"] = images[2][0]
    expect(request("PUT", f"/api/for-admin/materials/{m1}", webp_model, auth=True)[0], 200, "replace JPEG with WebP")
    expect(request("GET", "/api/materials/images/" + images[1][0])[0], 400, "old JPEG invalidated")
    response = request("GET", "/api/materials/images/" + images[2][0])
    expect(response[0], 200, "WebP GET")
    if response[2] != webp or response[1].get("Content-Type") != "image/webp":
        raise AssertionError("WebP mismatch")
    mark("IMG-11", "Замена на WebP сделала JPEG недоступным; возвращены исходные байты WebP")

    restart_server()
    for _ in range(2):
        response = request("GET", "/api/materials/images/" + images[2][0])
        expect(response[0], 200, "WebP after restart")
        if response[2] != webp:
            raise AssertionError("WebP changed after restart")
    mark("IMG-12", "После перезапуска WebP дважды получен с исходными байтами")

    base_invalid = material_model(prefix + "-validation", cat2, "raskroy")
    expect(request("POST", "/api/for-admin/materials", {}, auth=True)[0], 400, "empty required fields")
    for bad_category in (0, 2147483000):
        bad = copy.deepcopy(base_invalid); bad["categoryId"] = bad_category; bad["name"] += str(bad_category)
        expect(request("POST", "/api/for-admin/materials", bad, auth=True)[0], 400, "bad category")
    for field, value in (("sheetSizeId", 0), ("thicknessId", 0), ("kvM", -1), ("perimetrM", 0)):
        bad = copy.deepcopy(base_invalid); bad[field] = value; bad["name"] += "-" + field
        expect(request("POST", "/api/for-admin/materials", bad, auth=True)[0], 400, "bad dimensions")
    bad = copy.deepcopy(base_invalid); bad["price"] = 0
    expect(request("POST", "/api/for-admin/materials", bad, auth=True)[0], 400, "bad price")
    bad = copy.deepcopy(base_invalid)
    bad.update({"applicableToCutting": False, "applicableToPvhFacades": False, "applicableToEnamelFacades": False})
    expect(request("POST", "/api/for-admin/materials", bad, auth=True)[0], 400, "no calculator")
    duplicate = copy.deepcopy(base_invalid); duplicate["name"] = m2_model["name"]
    expect(request("POST", "/api/for-admin/materials", duplicate, auth=True)[0], 400, "duplicate material name")
    expect(request("GET", "/api/materials/2147483000")[0], 400, "missing material")
    expect(request("GET", "/api/for-admin/materials/categories/2147483000", auth=True)[0], 400, "missing category")
    expect(request("POST", "/api/for-admin/materials/change-order-col", {"id": m1, "direction": "SIDEWAYS"}, auth=True)[0], 400, "invalid direction")
    bad = copy.deepcopy(base_invalid); bad["name"] += "-guid"; bad["image"] = str(uuid.uuid4())
    expect(request("POST", "/api/for-admin/materials", bad, auth=True)[0], 400, "missing image GUID")
    mark("VAL-01—VAL-10", "Все проверенные некорректные модели и отсутствующие id/GUID вернули 400")

    counter_targets = (
        ("/api/for-admin/materials/categories", cat1),
        ("/api/for-admin/materials/sheet-sizes", primary_sheet_size_id),
        ("/api/for-admin/materials/manufacturers", primary_manufacturer_id)
    )
    counters_before_delete = [relation_counts(path, relation_id) for path, relation_id in counter_targets]
    expect(request("GET", "/api/materials/images/" + images[2][0])[0], 200, "WebP before delete")
    expect(delete_material(m1)[0], 200, "delete material")
    expect(request("GET", "/api/materials/images/" + images[2][0])[0], 400, "WebP after delete")
    expect(request("GET", f"/api/materials/{m1}")[0], 400, "deleted public card")
    expect(request("GET", f"/api/for-admin/materials/{m1}", auth=True)[0], 400, "deleted admin card")
    if m1 in {x["id"] for x in admin_materials()} or m1 in {x["id"] for x in public_materials()}:
        raise AssertionError("deleted material remained in a list")
    counters_after_delete = [relation_counts(path, relation_id) for path, relation_id in counter_targets]
    for before, after in zip(counters_before_delete, counters_after_delete):
        if after != (before[0] - 1, before[1]):
            raise AssertionError(f"physically deleted material remained in relation counters: {before} -> {after}")
    expect(delete_material(m1)[0], 400, "repeat material delete")
    for guid, _, _ in images:
        expect(request("GET", "/api/materials/images/" + guid)[0], 400, "final image GUID check")
    mark("MAT-09—MAT-11", "Проверены физическое удаление, отсутствие в списках и карточках, повторное удаление")
    mark("CAT-11, SIZE-10, MFR-11", "Физически удалённый материал исключён из счётчиков списков справочников")
    mark("IMG-13—IMG-14", "После удаления WebP недоступен; все три GUID недоступны")

    run_search_facet_scenarios(prefix, dimension_base)
    run_variant_scenarios(prefix, cat1, png)

    expect(delete_material(m2)[0], 200, "cleanup m2")
    expect(delete_material(m3)[0], 200, "cleanup m3")
    for category_id in (cat1, cat2, cat3):
        expect(request("DELETE", f"/api/for-admin/materials/categories/{category_id}", auth=True)[0], 200, "delete free category")
        created_categories.remove(category_id)
    for size_id in (primary_sheet_size_id, secondary_sheet_size_id):
        expect(request("DELETE", f"/api/for-admin/materials/sheet-sizes/{size_id}", auth=True)[0], 200, "delete free material sheet size")
        created_sheet_sizes.remove(size_id)
    for manufacturer_id in (primary_manufacturer_id, secondary_manufacturer_id):
        expect(request("DELETE", f"/api/for-admin/materials/manufacturers/{manufacturer_id}", auth=True)[0], 200, "delete free manufacturer")
        created_manufacturers.remove(manufacturer_id)
    mark("MFR-08—MFR-09", "Используемый производитель защищён от удаления; свободные производители удалены")
    mark("SIZE-07", "Свободные размеры материалов удалены")
    mark("CAT-10", "Свободные категории удалены")
    remaining_category_ids = {x["id"] for x in categories()}
    if {cat1, cat2, cat3} & remaining_category_ids:
        raise AssertionError("test categories remained in list")
    remaining_material_ids = {x["id"] for x in admin_materials()}
    if {m1, m2, m3} & remaining_material_ids:
        raise AssertionError("test materials remained in admin list")
    for guid, _, _ in images:
        expect(request("GET", "/api/materials/images/" + guid)[0], 400, "FIN image check")
    mark("FIN-01—FIN-03", "Материалы и категории удалены; GUID изображений недоступны")

    if history(search=prefix)["totalCount"] != 0:
        raise AssertionError("tester functional scenarios created catalog history events")
    mark("HIST-13", "Создание, чтение, изменение, удаление и очистка тестером не создали событий истории каталога")

    history_run_id = int(uuid.uuid4().hex[:7], 16) + 1
    history_prefix = f"codex-history-{history_run_id}"
    run_history_scenarios(
        history_prefix,
        dimension_base,
        manager_cookie,
        tester_cookie,
        credentials["manager"])
    mark("HIST-01—HIST-12", "Проверены изолированный аудит операций менеджера, отсутствие событий при сохранении без изменений и сортировке, телефон, снимки данных, авторизация, фильтры, поиск и пагинация")

    thickness_history_name = history_prefix + "-thickness"
    thickness_history_model = {"name": thickness_history_name, "value": dimension_base + 0.875}
    response = request("POST", "/api/for-admin/materials/thicknesses", thickness_history_model, auth_cookie=manager_cookie)
    expect(response[0], 200, "История создания толщины")
    thickness_history_id = json_body(response)["id"]
    created_thicknesses.append(thickness_history_id)
    thickness_history_model["name"] += "-updated"
    expect(request("PUT", f"/api/for-admin/materials/thicknesses/{thickness_history_id}", thickness_history_model,
                   auth_cookie=manager_cookie)[0], 200, "История переименования толщины")
    thickness_events = history(search=thickness_history_name, entityType="Thickness")
    equal(thickness_events["totalCount"], 2, "Число событий толщины")
    update_event = next(x for x in thickness_events["items"] if x["actionType"] == "Update")
    assert update_event["message"].count("→") == 1, "История толщины содержит неизменённые поля"
    assert thickness_history_name in update_event["message"] and thickness_history_model["name"] in update_event["message"]
    expect(request("PUT", f"/api/for-admin/materials/thicknesses/{thickness_history_id}", thickness_history_model,
                   auth_cookie=manager_cookie)[0], 200, "История сохранения толщины без изменений")
    equal(history(search=thickness_history_name)["totalCount"], 2, "Повторное сохранение не создаёт событие")
    expect(request("DELETE", f"/api/for-admin/materials/thicknesses/{thickness_history_id}", auth_cookie=manager_cookie)[0],
           200, "История удаления толщины")
    created_thicknesses.remove(thickness_history_id)
    thickness_events = history(search=thickness_history_name, entityType="Thickness")
    equal(thickness_events["totalCount"], 3, "История CRUD толщины")
    equal({x["actionType"] for x in thickness_events["items"]}, {"Create", "Update", "Delete"}, "Типы событий толщины")
    assert all(x["userPhone"] == credentials["manager"] for x in thickness_events["items"])
    mark("THICK-08", "История CRUD толщины, только изменённые поля, отсутствие события для no-op, инициатор")

    cleanup_response = request(
        "DELETE",
        f"/api/for-admin/catalog/history/test-runs/{history_run_id}",
        auth_cookie=tester_cookie)
    expect(cleanup_response[0], 200, "test history cleanup")
    if json_body(cleanup_response)["deletedCount"] != 15:
        raise AssertionError("test history cleanup deleted an unexpected number of events")
    if history(search=history_prefix)["totalCount"] != 0:
        raise AssertionError("test history events remained after cleanup")
    completed_history_run_id = history_run_id
    history_run_id = None
    mark("HIST-14", "Метод, доступный только тестеру, удалил все 15 изолированных событий истории; повторный поиск не вернул событий")

    for thickness_id in list(created_thicknesses):
        expect(request("DELETE", f"/api/for-admin/materials/thicknesses/{thickness_id}", auth=True)[0], 200, "Очистка толщин")
        created_thicknesses.remove(thickness_id)
    assert not any((created_materials, created_categories, created_sheet_sizes, created_manufacturers, created_thicknesses)), "Остались созданные тестовые сущности"
    equal(public_query("search", {"search": prefix})["totalCount"], 0, "Материалы тестового префикса удалены")
    equal(history(search=prefix)["totalCount"], 0, "История основного прогона отсутствует")
    for endpoint in ("categories", "manufacturers", "sheet-sizes", "thicknesses"):
        items = json_body(request("GET", "/api/for-admin/materials/" + endpoint, auth=True))["items"]
        assert not any(prefix in x["name"] or history_prefix in x["name"] for x in items), "Остались тестовые справочники"
    mark("FIN-04", "Все списки тестовых сущностей пусты, префиксы отсутствуют в справочниках, материалах и истории")
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