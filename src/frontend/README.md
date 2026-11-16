# 前端说明（Vue 3 + Vite + TypeScript）

## 技术栈

- Vue 3 + Composition API
- Vite 7
- TypeScript
- Element Plus
- Axios

## 目录结构

```text
src/frontend/
├─ public/                 # 静态资源
├─ src/
│  ├─ api/                 # Axios 实例和联系人接口
│  ├─ components/          # 工具栏、表格、编辑弹窗
│  ├─ composables/         # useContacts：列表、分页、收藏、导入导出
│  ├─ types/               # 联系人类型、联系方式枚举元数据
│  ├─ utils/               # 文件下载等工具
│  ├─ views/               # AddressBookView 页面
│  ├─ styles/              # 全局样式与主题变量
│  ├─ App.vue
│  ├─ main.ts
│  └─ env.d.ts
├─ index.html
├─ package.json
├─ tsconfig.json
└─ vite.config.ts
```

## 安装与启动

```powershell
cd src/frontend
npm install
npm run dev
```

打开 `http://127.0.0.1:5173`。

开发服务器把 `/api` 代理到 `http://127.0.0.1:5012`，因此需要先启动后端。

## 构建

```powershell
npm run build
npm run preview
```

- 构建产物：`dist/`
- 预览地址：`http://127.0.0.1:4173`

生产环境如果前后端不同源，设置 API 地址：

```powershell
$env:VITE_API_BASE_URL = "https://your-api.example.com/api"
npm run build
```

也可以复制 `src/frontend/.env.example` 为 `.env.local` 后修改。

## 页面功能

- 搜索姓名、公司、备注和联系方式
- 收藏联系人、仅看收藏
- 新增、编辑、删除联系人
- 一个联系人维护多条联系方式，支持主联系方式
- 点击联系方式标签复制
- 导出当前筛选结果为 Excel
- 导入 Excel 并自动刷新列表

## 类型检查

```powershell
npm run type-check
```
