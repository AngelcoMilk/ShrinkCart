# 附录 A：Thunderstore API 使用准则

> 原为《R.E.P.O. Mod 开发通用规范》§10，2.0 版起移入附录：它是发布平台的参考资料，不属于 mod 代码规范。

官方 Swagger UI 位于：

```text
https://thunderstore.io/api/docs/
```

该页面实际读取的机器可读 schema 是：

```text
https://thunderstore.io/api/docs/?format=openapi
```

返回类型为 `application/openapi+json`，当前 schema 是 Swagger/OpenAPI 2.0，标题为 `Thunderstore API v1`。官方 schema 自己明确警告：**自动生成且不完全准确**。因此：

- API 集成必须用实际响应、状态码和官方网页行为再次验证，不能只依赖生成的 schema。
- 不要猜测 `/swagger.json`、`/openapi.json` 或 `/api/schema/`，这些路径不是文档 UI 当前使用的 schema。
- schema 顶层声明了 Basic Authentication，但这不代表每个读取端点都需要认证；按每个操作的实际行为测试。
- 不要在源码、manifest、日志或 issue 中提交用户名、密码、session、token 或 Authorization header。

## A.1 接口稳定性分类

当前 schema 主要包含：

- `/api/v1/package/` 与按社区划分的 `/c/{community_identifier}/api/v1/package/`：包列表与包读取。
- `/api/v1/package-metrics/...`：包与版本指标。
- `/api/v1/current-user/info/`、评分和 bot 管理操作：用户相关或写操作，按需认证、最小授权。
- `/api/experimental/...`：社区、frontend、package、wiki、submission、validation、usermedia 等实验接口。
- `/api/cyberstorm/...`：Cyberstorm 前端相关接口。

开发准则：

1. 优先使用能满足需求的 `/api/v1/` 读取接口。
2. `/api/experimental/` 和 `/api/cyberstorm/` 视为不稳定的实现细节。调用方必须能容忍字段增删、状态码变化和接口迁移。
3. 上传发布、异步 submission、wiki、评分、审核等写操作，不得在构建脚本中默认执行；必须显式启用，并保护好凭据。
4. HTTP 调用要设置超时、清晰的 User-Agent、有限次数的重试和失败降级；遇到 `429` 和服务端错误要退让。
5. 公开元数据要缓存；不要在游戏 `Update()` 或高频事件中请求 Thunderstore。
6. 发布前重新下载 schema，复核所用的端点；不要把下载的动态 schema 当作仓库里的永久事实。

示例（只读取 schema）：

```powershell
Invoke-RestMethod -Uri "https://thunderstore.io/api/docs/?format=openapi"
```

官方参考：

- API docs: <https://thunderstore.io/api/docs/>
- Creating a Package: <https://wiki.thunderstore.io/mods/creating-a-package>
