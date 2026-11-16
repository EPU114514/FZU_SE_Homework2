# 通讯录管理系统

本项目是软件工程课程作业，实现了一个基于 Web 的通讯录管理系统。项目采用前后端分离结构：前端使用 Vue 3，后端使用 .NET Web API，支持联系人收藏、多联系方式管理、搜索分页以及 Excel 导入导出。

## 功能说明

- 收藏联系人，并提供“仅看收藏”筛选。
- 一个联系人可以保存电话、邮箱、微信、QQ、地址、社交账号和其他联系方式。
- 每种联系方式可以设置一个主联系方式。
- 支持 Excel 文件导入和导出，导入时可跳过重复联系人。
- 支持按姓名、公司、备注和联系方式搜索，并支持分页。
- 提供表单校验、统一的接口错误提示和 Swagger 接口文档。
- 数据默认保存为 JSON 文件，也可以切换为 MongoDB。

## 技术栈

- 后端：.NET 11 Web API、Swashbuckle、ClosedXML、MongoDB
- 前端：Vue 3、TypeScript、Vite、Element Plus、Axios
- 部署：Docker Compose、Nginx

## 项目结构

```text
Homework2/
├─ Homework2.slnx
├─ README.md
├─ docker-compose.yml
└─ src/
   ├─ backend/
   │  ├─ Homework2.Api/             # Web API、控制器、Swagger
   │  ├─ Homework2.Application/     # 服务接口、数据传输对象、业务规则
   │  ├─ Homework2.Domain/          # 联系人实体和枚举
   │  └─ Homework2.Infrastructure/  # JSON/MongoDB 仓储、Excel 导入导出
   └─ frontend/
      ├─ src/
      │  ├─ api/                    # Axios 客户端和接口封装
      │  ├─ components/             # 工具栏、联系人表格、编辑弹窗
      │  ├─ composables/            # 联系人列表和操作逻辑
      │  ├─ types/                  # TypeScript 类型定义
      │  ├─ utils/                  # 文件下载等工具
      │  ├─ views/                  # 页面
      │  └─ styles/                 # 全局样式
      ├─ public/
      ├─ index.html
      ├─ package.json
      └─ vite.config.ts
```

## 运行环境

- .NET SDK 11.0 或更高版本
- Node.js 20.19 或更高版本，以及 npm
- MongoDB 6.0 或更高版本（可选）

## 启动后端

在项目根目录执行：

```powershell
dotnet restore
dotnet run --project src/backend/Homework2.Api/Homework2.Api.csproj --launch-profile http
```

启动后可以访问：

- API：`http://localhost:5012`
- Swagger：`http://localhost:5012/swagger`

首次写入联系人时，程序会自动创建 `src/backend/Homework2.Api/data/contacts.json`。后端的具体配置和接口说明见 [`src/backend/README.md`](src/backend/README.md)。

## 启动前端

另开一个终端执行：

```powershell
cd src/frontend
npm install
npm run dev
```

浏览器访问 `http://127.0.0.1:5173`。开发环境下，Vite 会把 `/api` 请求代理到 `http://127.0.0.1:5012`，因此需要先启动后端。前端说明见 [`src/frontend/README.md`](src/frontend/README.md)。

## 构建

后端：

```powershell
dotnet publish src/backend/Homework2.Api/Homework2.Api.csproj -c Release -o publish/backend
```

前端：

```powershell
cd src/frontend
npm run build
```

前端构建结果位于 `src/frontend/dist`。如果前后端部署在不同域名下，可以在构建前指定后端地址：

```powershell
$env:VITE_API_BASE_URL = https://example.com/api
npm run build
```

## Docker 部署

项目根目录提供了 `docker-compose.yml`。启动 Docker 后，在根目录执行：

```powershell
docker compose up -d --build
```

默认访问地址为 `http://localhost:8080`，Swagger 地址为 `http://localhost:8080/swagger`。联系人数据保存在 Docker 数据卷 `homework2-data` 中。

停止服务：

```powershell
docker compose down
```

如果需要删除联系人数据，可执行 `docker compose down -v`。该命令会删除数据卷，请谨慎使用。

## 存储配置

默认使用 JSON 文件存储，配置文件为 `src/backend/Homework2.Api/appsettings.json`：

```json
Storage: {
  Provider: Json,
  Json: { FilePath: data/contacts.json }
}
```

切换为 MongoDB 时，将配置修改为：

```json
Storage: {
  Provider: Mongo,
  Mongo: {
    ConnectionString: mongodb://localhost:27017,
    Database: homework2,
    Collection: contacts
  }
}
```

## 接口列表

| 方法 | 路径 | 说明 |
| --- | --- | --- |
| GET | `/api/contacts` | 分页查询联系人，支持 `search`、`favorite`、`page`、`pageSize` |
| GET | `/api/contacts/{id}` | 查询单个联系人 |
| POST | `/api/contacts` | 新增联系人 |
| PUT | `/api/contacts/{id}` | 更新联系人及其联系方式 |
| PATCH | `/api/contacts/{id}/favorite` | 设置收藏状态 |
| DELETE | `/api/contacts/{id}` | 删除联系人 |
| POST | `/api/contacts/{id}/methods` | 新增联系方式 |
| PUT | `/api/contacts/{id}/methods/{methodId}` | 更新联系方式 |
| DELETE | `/api/contacts/{id}/methods/{methodId}` | 删除联系方式 |
| GET | `/api/contacts/export` | 导出 Excel 文件 |
| POST | `/api/contacts/import` | 导入 Excel 文件，使用 `multipart/form-data`，字段名为 `file` |

## Excel 文件格式

导出的 Excel 文件中，每个联系人占一行，包含以下列：

- `Name`、`Company`、`Favorite`、`Notes`
- `Phone 1`、`Phone 2` 等电话列
- `Email 1` 等邮箱列
- `WeChat`、`QQ`、`Address`、`Social`、`Other`

导入时按表头名称匹配，也支持中文表头：`姓名`、`公司`、`收藏`、`备注`、`电话`、`邮箱`、`微信`、`地址`、`社交`、`其他`。收藏列可以使用 `Yes/No`、`true/false`、`1/0` 或 `是/否`。

## 常见问题

- 页面提示请求失败时，请检查后端是否已在 `5012` 端口启动。
- 后端端口被占用时，可以通过 `--urls http://127.0.0.1:5013` 更换端口，并同步修改前端代理地址。
- 不需要 MongoDB 时，将 `Storage:Provider` 保持为 `Json` 即可。
