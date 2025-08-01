<p align="center">
  <img src="J8.png" width="100">
  <span style="font-size: 32px;">➡️</span>
  <img src="J8.ico" width="100">
</p>

# 💩 HonorPCManagerisJ8

## 荣耀电脑管家就是一坨屎

---

## ❓ 为什么要替代它？

* 💻 唯一价值：设置硬件参数（如充电阈值）
* 🔒 驱动加密 & 鉴权，限制使用
* 🔔 弹窗永远关不掉，驱动升级提示烦人
* 🛡️ 奇安信后台服务常驻，几乎没用
* 🧹 功能臃肿，占内存资源

---

## ✅ 这个程序可以做什么？

* 🧠 **开机自启**，自动设置硬件参数（如充电上限）
* 🔄 **退出自动恢复**（如恢复充电限制）
* ⚙️ **支持自定义配置文件**
* 🧼 **无弹窗、无广告**，仅托盘图标，安静运行

---

> 🧪 本项目为**功能可行性测试**，抛砖引玉，欢迎大佬写出更优雅的实现！

---

## 🔧 配置文件示例

```yaml
startup: false         # 是否开机自启
debug: true            # 是否显示窗口
timeout: 3600000       # 进行EC覆写的间隔（毫秒）
wait: 100              # 每步EC操作的间隔（毫秒）

settings:              # 启动时写入的EC值
  - 92: 00             # 充电阈值低位，小端序 25600 = 0x6400，对应100%
  - 93: 32             # 充电阈值高位，当前为 0x0032 = 50%
  - 24: 00             # 键盘灯超时设置（低位）
  - 25: 00             # 键盘灯超时设置（高位）
  - 21: 23             # 功能键模式：默认使用F键

exit:                  # 退出时写入的EC值（如恢复默认设置）
  - 92: 00             # 充电阈值低位，100%
  - 93: 64             # 充电阈值高位，100%
```
