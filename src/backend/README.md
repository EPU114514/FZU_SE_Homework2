# 后端说明（.NET 11 Web API）

## 项目分层

| 项目 | 职责 |
| --- | --- |
| `Homework2.Domain` | `Contact`、`ContactMethod` 实体和 `ContactMethodType` 枚举，不依赖框架 |
| `Homework2.Application` | DTO、`IContactService`、`IContactRepository`、`IExcelService` 以及业务规则 |
| `Homework2.Infrastructure` | JSON 文件仓储、MongoDB 仓储、ClosedXML Excel 导入导出 |
| `Homework2.Api` | 控制器、依赖注入、Swagger、全局异常处理 |

上层只依赖接口，具体存储和 Excel 实现放在 Infrastructure，便于替换和测试。

## 启动

在仓库根目录执行：

```powershell
dotnet restore
dotnet run --project src/backend/Homework2.Api/Homework2.Api.csproj --launch-profile http
```

默认地址：

- API：`http://localhost:5012`
- Swagger：`http://localhost:5012/swagger`

## 配置

`Homework2.Api/appsettings.json`：

```json
"Storage": {
  "Provider": "Json",
  "Json": { "FilePath": "data/contacts.json" },
  "Mongo": {
    "ConnectionString": "mongodb://localhost:27017",
    "Database": "homework2",
    "Collection": "contacts"
  }
}
```

- `Provider = Json`：把联系人保存到内容根目录下的 `data/contacts.json`
- `Provider = Mongo`：使用 MongoDB，需要本机或远程存在可访问的 `mongod`

## 接口

| 方法 | 路径 | 说明 |
| --- | --- | --- |
| GET | `/api/contacts?search=&favorite=&page=1&pageSize=20` | 分页查询，收藏优先排序 |
| GET | `/api/contacts/{id}` | 查询单个联系人 |
| POST | `/api/contacts` | 新增联系人及联系方式 |
| PUT | `/api/contacts/{id}` | 更新联系人及全部联系方式 |
| PATCH | `/api/contacts/{id}/favorite` | 设置收藏：`{"isFavorite": true}` |
| DELETE | `/api/contacts/{id}` | 删除联系人 |
| POST | `/api/contacts/{id}/methods` | 新增一条联系方式 |
| PUT | `/api/contacts/{id}/methods/{methodId}` | 更新一条联系方式 |
| DELETE | `/api/contacts/{id}/methods/{methodId}` | 删除一条联系方式 |
| GET | `/api/contacts/export` | 导出 `.xlsx` |
| POST | `/api/contacts/import?skipDuplicates=true` | 导入 `.xlsx`，表单字段 `file` |

请求示例见 `Homework2.Api/Homework2.http`。

## Excel 格式

导出列：

- `Name`、`Company`、`Favorite`、`Notes`
- `Phone 1..n`、`Email 1..n`、`WeChat`、`QQ`、`Address`、`Social`、`Other`

导入按表头名称识别，支持中文表头。`Favorite` 接受 `Yes/No`、`true/false`、`1/0`、`是`。

## 运行验证

```powershell
dotnet build src/backend/Homework2.Api/Homework2.Api.csproj
```

编译应显示 0 错误、0 警告。
