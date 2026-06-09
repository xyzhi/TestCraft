# 项目规则

- 所有代码用 UTF-8 编码。
- 我提问的时候，没有允许不可以直接执行；工作区的内容只要开始执行，就不用审批。
- 尽量用中文回答。
- Git 账号：`xyzhi`
- Git 邮箱：`xxxyyyzzzhi@163.com`

# VR 规则

- 当前项目判断是否处于 VR 模式，统一使用 `AirStrikeGame.playerController.IsVRActive`。
- `IsVRActive` 返回 `PlayerController` 内部的 `useVrInput`。
- `useVrInput` 当前规则为：`XRSettings.isDeviceActive || IsEditorSimulatorActive()`。
- 编辑器模拟器模式下，不再依赖旧的 `EnableVRController`、`AutoDetectXR` 字段来判断 VR 状态。
