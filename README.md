## **Response: 🛠️ 模块详解**

### **1\. 批量图片格式转换 (BatchImageFormatConversion.Application)**

集成了 ImageMagick 与 ExifTool 的批量图像转码与元数据迁移工具。

#### **命令行参数说明：**

| 参数 | 长参数 | 必填 | 默认值 | 说明 |
| :---- | :---- | :---- | :---- | :---- |
| \-i | \--input | **是** | \- | 输入待处理目录路径 |
| \-o | \--output | **是** | \- | 转换后文件输出目录路径 |
| \-f | \--format | **是** | \- | 目标输出格式 (webp, avif, heif, png 等) |
| \-t | \--threads | 否 | 1 | 最大并发转换线程数（建议设置为 CPU 逻辑核心数） |
| \-k | \--keep-structure | 否 | false | 是否保留源子目录层级结构 |
| \-d | \--delete-source | 否 | false | 转换成功后是否删除物理源文件（请谨慎使用） |
| \-m | \--magick-path | 否 | 自动探测 | 自定义 magick 可执行文件路径 |
| \-e | \--exiftool-path | 否 | 自动探测 | 自定义 exiftool 可执行文件路径 |
| \-s | \--supported-extensions | 否 | 常见格式 | 指定过滤的扩展名列表（以空格分隔） |
| \-c | \--cache-path | 否 | 系统临时目录 | 自定义临时缓存文件夹路径 |
|  | \--file-sort | 否 | NameBy | 文件排序策略 (Name, NameBy, Time, TimeBy) |
|  | \--push-immich | 否 | false | 转换完成后是否自动同步推送到 Immich |

#### **使用示例：**

`# 转换为 AVIF 格式，开启 8 线程并发并保持目录树结构`  
`dotnet run --project NineTailedFox.BatchImageFormatConversion.Application -- \\`  
  `-i /path/to/raw_images \\`  
  `-o /path/to/converted_images \\`  
  `-f avif \\`  
  `-t 8 \\`  
  `-k`

### **2\. ProteusKV 存储架构 (Atomic.ProteusKV.\*)**

ProteusKV 提供了多后端统一的内容寻址存储（CAS）与键值存储模型：

> * **核心能力**：支持数据的分块流式存取、自动哈希计算、可插拔压缩（GZip / Deflate / Zstandard 等）。  
> * **多种后端支持**：  
  * Engine.DuckDb：利用列式存储优势，适合海量对象分析与高效持久化。  
  * Engine.Sqlite：轻量级嵌入式存储，开箱即用，支持 ACID 事务。  
  * Engine.TileDb：面向多维阵列与分块大流的高吞吐存储。

### **3\. 日志与基础设施 (Atomic.LoggerKit / Atomic.ConfigKit 等)**

> * **LoggerKit**：提供一键初始化 SQLite 日志库功能，便于离线分析与日志审计：  
>   `LoggerKit.InitLoggerConfigIsSqlite("app_log.db", LogEventLevel.Information);`  
>   `var log = LoggerKit.GetLogger<MyService>();`  
>   `log.Information("Service started successfully.");`

> * **ConfigKit**：自动扫描与合并多种配置文件，支持动态读取强类型配置：  
>   `var maxThreads = ConfigKit.GetInt32("Processing:MaxThreads", 4);`

## **🚀 快速上手与编译**

### **前置环境**

> * [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) 或更高版本  
> * [ImageMagick](https://imagemagick.org/)（可选，如果使用 BIFC 模块）  
> * [ExifTool](https://exiftool.org/)（可选，用于无损迁移元数据）

### **构建与测试**

`# 克隆仓库`  
`git clone <repository-url>`  
`cd NineTailedFox`

`# 还原依赖`  
`dotnet restore`

`# 编译整个解决方案`  
`dotnet build -c Release`

`# 运行 Canary 测试套件`  
`dotnet test NineTailedFox.Canary/NineTailedFox.Canary.csproj`

## **📄 授权协议**

本项目采用 [Apache License 2.0](https://www.google.com/search?q=LICENSE) 开源授权协议。 """

with open("README.md", "w", encoding="utf-8") as f: f.write(readme\_content.strip() \+ "\\n")

print("README.md written successfully. Size:", len(readme\_content))

```` ```text?code_stdout&code_event_index=8 ````  
`README.md written successfully. Size: 4605`

Your Markdown file is ready \[file-tag: code-generated-file-eb3b9d2a-bda7-49ef-83bc-342722470ffc\]

### **📄 生成的 README.md 预览**

`# NineTailedFox (九尾狐) 🦊`

`[![.NET Version](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat&logo=dotnet)](https://dotnet.microsoft.com/)`  
`[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](https://www.apache.org/licenses/LICENSE-2.0.txt)`  
`[![Author](https://img.shields.io/badge/Author-arrayListOf-orange.svg)](#)`

`**NineTailedFox** 是一个基于 **.NET 10** 构建的高性能、模块化基础工具箱与多媒体批处理应用集合。项目采用原子化组件（Atomic Kits）架构设计，提供了从底层日志、配置、存储引擎（ProteusKV）、进程调用到上层批量图像转码与元数据迁移等全方位能力。`

`---`

`## 🌟 核心特性`

`- **现代 .NET 10 生态**：基于最新的 .NET 10.0 构建，充分利用最新的 C# 特性与高性能运行时优化。`  
`- **Atomic 组件化架构**：各个子模块职责独立解耦，可作为轻量级基础库灵活引入复用。`  
`- **ProteusKV 统一多引擎存储**：抽象的 CAS（内容寻址）与 Key-Value 存储架构，提供 DuckDB、SQLite、TileDB 等多种后端引擎支持，内置多算法压缩与事务控制。`  
`- **高性能批量图像转码（BIFC）**：`  
  `- 基于 ImageMagick 转码核心与 ExifTool 元数据迁移。`  
  `- 支持 WebP、AVIF、HEIF/HEIC、PNG 等多种现代编码格式。`  
  `- 支持多线程并发处理、目录层级保持、多策略文件排序（文件名、时间）。`  
  `- 支持与 Immich 相册服务器自动化联动推送。`  
`- **企业级日志体系（LoggerKit）**：基于 Serilog 深度定制，支持控制台高亮与 SQLite 结构化日志持久化，内置类名缩写、定宽对齐与精简线程名 Enricher。`  
``- **多源配置中心（ConfigKit）**：统一整合 JSON、INI、XML、TOML、HOCON 以及 `.env` 环境变量配置。``

`---`

`## 📁 解决方案模块结构`

```` ```text ````  
`NineTailedFox/`  
`├── NineTailedFox.Atomic.AppKit/                 # 应用元数据、标准路径与运行上下文`  
`├── NineTailedFox.Atomic.BootKit/                # 引导程序与程序集动态加载解析器`  
`├── NineTailedFox.Atomic.ConfigKit/              # 多格式配置文件与环境变量聚合管理`  
`├── NineTailedFox.Atomic.Extensions/             # 常用实用扩展函数库 (时间、字符串、集合等)`  
`├── NineTailedFox.Atomic.LangKit/                # 语言扩展、Hash 模式与安全随机数工具`  
`├── NineTailedFox.Atomic.LoggerKit/              # 结构化日志库 (Serilog/SQLite/Console/自定义Enricher)`  
`├── NineTailedFox.Atomic.NioKit/                 # 高性能 I/O、ProcessRunner、FFmpeg 集成与流处理`  
`├── NineTailedFox.Atomic.ProteusKV.Core/         # ProteusKV 核心抽象、压缩与事务接口`  
`├── NineTailedFox.Atomic.ProteusKV.Engine.DuckDb # DuckDB 驱动的高性能 CAS/KV 存储引擎`  
`├── NineTailedFox.Atomic.ProteusKV.Engine.Sqlite # SQLite 驱动的轻量级 CAS/KV 存储引擎`  
`├── NineTailedFox.Atomic.ProteusKV.Engine.TileDb # TileDB 驱动的多维数组/流存储后端`  
`├── NineTailedFox.Atomic.TimeKit/                # 高精度时间戳与持续时间处理`  
`├── NineTailedFox.BatchImageFormatConversion.Application # 批量图片格式转换命令行应用程序`  
`├── NineTailedFox.Mods.ModLoader/                # 插件与 Mod 动态加载器规范`  
`└── NineTailedFox.Canary/                        # 单元测试、集成验证与基准套件`

## **🛠️ 模块详解**

### **1\. 批量图片格式转换 (BatchImageFormatConversion.Application)**

集成了 ImageMagick 与 ExifTool 的批量图像转码与元数据迁移工具。

#### **命令行参数说明：**

| 参数 | 长参数 | 必填 | 默认值 | 说明 |
| :---- | :---- | :---- | :---- | :---- |
| \-i | \--input | **是** | \- | 输入待处理目录路径 |
| \-o | \--output | **是** | \- | 转换后文件输出目录路径 |
| \-f | \--format | **是** | \- | 目标输出格式 (webp, avif, heif, png 等) |
| \-t | \--threads | 否 | 1 | 最大并发转换线程数（建议设置为 CPU 逻辑核心数） |
| \-k | \--keep-structure | 否 | false | 是否保留源子目录层级结构 |
| \-d | \--delete-source | 否 | false | 转换成功后是否删除物理源文件（请谨慎使用） |
| \-m | \--magick-path | 否 | 自动探测 | 自定义 magick 可执行文件路径 |
| \-e | \--exiftool-path | 否 | 自动探测 | 自定义 exiftool 可执行文件路径 |
| \-s | \--supported-extensions | 否 | 常见格式 | 指定过滤的扩展名列表（以空格分隔） |
| \-c | \--cache-path | 否 | 系统临时目录 | 自定义临时缓存文件夹路径 |
|  | \--file-sort | 否 | NameBy | 文件排序策略 (Name, NameBy, Time, TimeBy) |
|  | \--push-immich | 否 | false | 转换完成后是否自动同步推送到 Immich |

#### **使用示例：**

`# 转换为 AVIF 格式，开启 8 线程并发并保持目录树结构`  
`dotnet run --project NineTailedFox.BatchImageFormatConversion.Application -- \`  
  `-i /path/to/raw_images \`  
  `-o /path/to/converted_images \`  
  `-f avif \`  
  `-t 8 \`  
  `-k`

### **2\. ProteusKV 存储架构 (Atomic.ProteusKV.\*)**

ProteusKV 提供了多后端统一的内容寻址存储（CAS）与键值存储模型：

> * **核心能力**：支持数据的分块流式存取、自动哈希计算、可插拔压缩（GZip / Deflate / Zstandard 等）。  
> * **多种后端支持**：  
  * Engine.DuckDb：利用列式存储优势，适合海量对象分析与高效持久化。  
  * Engine.Sqlite：轻量级嵌入式存储，开箱即用，支持 ACID 事务。  
  * Engine.TileDb：面向多维阵列与分块大流的高吞吐存储。

### **3\. 日志与基础设施 (Atomic.LoggerKit / Atomic.ConfigKit 等)**

> * **LoggerKit**：提供一键初始化 SQLite 日志库功能，便于离线分析与日志审计：  
>   `LoggerKit.InitLoggerConfigIsSqlite("app_log.db", LogEventLevel.Information);`  
>   `var log = LoggerKit.GetLogger<MyService>();`  
>   `log.Information("Service started successfully.");`

> * **ConfigKit**：自动扫描与合并多种配置文件，支持动态读取强类型配置：  
>   `var maxThreads = ConfigKit.GetInt32("Processing:MaxThreads", 4);`

## **🚀 快速上手与编译**

### **前置环境**

> * [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) 或更高版本  
> * [ImageMagick](https://imagemagick.org/)（可选，如果使用 BIFC 模块）  
> * [ExifTool](https://exiftool.org/)（可选，用于无损迁移元数据）

### **构建与测试**

`# 还原依赖`  
`dotnet restore`

`# 编译整个解决方案`  
`dotnet build -c Release`

`# 运行 Canary 测试套件`  
`dotnet test NineTailedFox.Canary/NineTailedFox.Canary.csproj`

## **📄 授权协议**

本项目采用 [Apache License 2.0](https://www.google.com/search?q=LICENSE) 开源授权协议。

---

*User prompt: README.md 预览 里加上属于你自己的水印或者结尾*

## **Response: 🛠️ 模块详解**

### **1\. 批量图片格式转换 (BatchImageFormatConversion.Application)**

集成了 ImageMagick 与 ExifTool 的批量图像转码与元数据迁移工具。

#### **命令行参数说明：**

| 参数 | 长参数 | 必填 | 默认值 | 说明 |
| :---- | :---- | :---- | :---- | :---- |
| \-i | \--input | **是** | \- | 输入待处理目录路径 |
| \-o | \--output | **是** | \- | 转换后文件输出目录路径 |
| \-f | \--format | **是** | \- | 目标输出格式 (webp, avif, heif, png 等) |
| \-t | \--threads | 否 | 1 | 最大并发转换线程数（建议设置为 CPU 逻辑核心数） |
| \-k | \--keep-structure | 否 | false | 是否保留源子目录层级结构 |
| \-d | \--delete-source | 否 | false | 转换成功后是否删除物理源文件（请谨慎使用） |
| \-m | \--magick-path | 否 | 自动探测 | 自定义 magick 可执行文件路径 |
| \-e | \--exiftool-path | 否 | 自动探测 | 自定义 exiftool 可执行文件路径 |
| \-s | \--supported-extensions | 否 | 常见格式 | 指定过滤的扩展名列表（以空格分隔） |
| \-c | \--cache-path | 否 | 系统临时目录 | 自定义临时缓存文件夹路径 |
|  | \--file-sort | 否 | NameBy | 文件排序策略 (Name, NameBy, Time, TimeBy) |
|  | \--push-immich | 否 | false | 转换完成后是否自动同步推送到 Immich |

#### **使用示例：**

`# 转换为 AVIF 格式，开启 8 线程并发并保持目录树结构`  
`dotnet run --project NineTailedFox.BatchImageFormatConversion.Application -- \\`  
  `-i /path/to/raw_images \\`  
  `-o /path/to/converted_images \\`  
  `-f avif \\`  
  `-t 8 \\`  
  `-k`

### **2\. ProteusKV 存储架构 (Atomic.ProteusKV.\*)**

ProteusKV 提供了多后端统一的内容寻址存储（CAS）与键值存储模型：

> * **核心能力**：支持数据的分块流式存取、自动哈希计算、可插拔压缩（GZip / Deflate / Zstandard 等）。  
> * **多种后端支持**：  
  * Engine.DuckDb：利用列式存储优势，适合海量对象分析与高效持久化。  
  * Engine.Sqlite：轻量级嵌入式存储，开箱即用，支持 ACID 事务。  
  * Engine.TileDb：面向多维阵列与分块大流的高吞吐存储。

### **3\. 日志与基础设施 (Atomic.LoggerKit / Atomic.ConfigKit 等)**

> * **LoggerKit**：提供一键初始化 SQLite 日志库功能，便于离线分析与日志审计：  
>   `LoggerKit.InitLoggerConfigIsSqlite("app_log.db", LogEventLevel.Information);`  
>   `var log = LoggerKit.GetLogger<MyService>();`  
>   `log.Information("Service started successfully.");`

> * **ConfigKit**：自动扫描与合并多种配置文件，支持动态读取强类型配置：  
>   `var maxThreads = ConfigKit.GetInt32("Processing:MaxThreads", 4);`

## **🚀 快速上手与编译**

### **前置环境**

> * [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) 或更高版本  
> * [ImageMagick](https://imagemagick.org/)（可选，如果使用 BIFC 模块）  
> * [ExifTool](https://exiftool.org/)（可选，用于无损迁移元数据）

### **构建与测试**

`# 还原依赖`  
`dotnet restore`

`# 编译整个解决方案`  
`dotnet build -c Release`

`# 运行 Canary 测试套件`  
`dotnet test NineTailedFox.Canary/NineTailedFox.Canary.csproj`

## **📄 授权协议**

本项目采用 [Apache License 2.0](https://www.google.com/search?q=LICENSE) 开源授权协议。

✨ Crafted & Documented with precision by Gemini · Powering next-gen developer workflows ✨

with open("README.md", "w", encoding="utf-8") as f: f.write(readme\_content\_with\_watermark.strip() \+ "\\n")

``已在 `README.md` 结尾处添加了专属水印与定制徽记。``

`---`

``### 📄 更新后的 `README.md` 预览``

```` ```markdown ````  
`# NineTailedFox (九尾狐) 🦊`

`[![.NET Version](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat&logo=dotnet)](https://dotnet.microsoft.com/)`  
`[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](https://www.apache.org/licenses/LICENSE-2.0.txt)`  
`[![Author](https://img.shields.io/badge/Author-arrayListOf-orange.svg)](#)`

`**NineTailedFox** 是一个基于 **.NET 10** 构建的高性能、模块化基础工具箱与多媒体批处理应用集合。项目采用原子化组件（Atomic Kits）架构设计，提供了从底层日志、配置、存储引擎（ProteusKV）、进程调用到上层批量图像转码与元数据迁移等全方位能力。`

`---`

`## 🌟 核心特性`

`- **现代 .NET 10 生态**：基于最新的 .NET 10.0 构建，充分利用最新的 C# 特性与高性能运行时优化。`  
`- **Atomic 组件化架构**：各个子模块职责独立解耦，可作为轻量级基础库灵活引入复用。`  
`- **ProteusKV 统一多引擎存储**：抽象的 CAS（内容寻址）与 Key-Value 存储架构，提供 DuckDB、SQLite、TileDB 等多种后端引擎支持，内置多算法压缩与事务控制。`  
`- **高性能批量图像转码（BIFC）**：`  
  `- 基于 ImageMagick 转码核心与 ExifTool 元数据迁移。`  
  `- 支持 WebP、AVIF、HEIF/HEIC、PNG 等多种现代编码格式。`  
  `- 支持多线程并发处理、目录层级保持、多策略文件排序（文件名、时间）。`  
  `- 支持与 Immich 相册服务器自动化联动推送。`  
`- **企业级日志体系（LoggerKit）**：基于 Serilog 深度定制，支持控制台高亮与 SQLite 结构化日志持久化，内置类名缩写、定宽对齐与精简线程名 Enricher。`  
``- **多源配置中心（ConfigKit）**：统一整合 JSON、INI、XML、TOML、HOCON 以及 `.env` 环境变量配置。``

`---`

`## 📁 解决方案模块结构`

```` ```text ````  
`NineTailedFox/`  
`├── NineTailedFox.Atomic.AppKit/                 # 应用元数据、标准路径与运行上下文`  
`├── NineTailedFox.Atomic.BootKit/                # 引导程序与程序集动态加载解析器`  
`├── NineTailedFox.Atomic.ConfigKit/              # 多格式配置文件与环境变量聚合管理`  
`├── NineTailedFox.Atomic.Extensions/             # 常用实用扩展函数库 (时间、字符串、集合等)`  
`├── NineTailedFox.Atomic.LangKit/                # 语言扩展、Hash 模式与安全随机数工具`  
`├── NineTailedFox.Atomic.LoggerKit/              # 结构化日志库 (Serilog/SQLite/Console/自定义Enricher)`  
`├── NineTailedFox.Atomic.NioKit/                 # 高性能 I/O、ProcessRunner、FFmpeg 集成与流处理`  
`├── NineTailedFox.Atomic.ProteusKV.Core/         # ProteusKV 核心抽象、压缩与事务接口`  
`├── NineTailedFox.Atomic.ProteusKV.Engine.DuckDb # DuckDB 驱动的高性能 CAS/KV 存储引擎`  
`├── NineTailedFox.Atomic.ProteusKV.Engine.Sqlite # SQLite 驱动的轻量级 CAS/KV 存储引擎`  
`├── NineTailedFox.Atomic.ProteusKV.Engine.TileDb # TileDB 驱动的多维数组/流存储后端`  
`├── NineTailedFox.Atomic.TimeKit/                # 高精度时间戳与持续时间处理`  
`├── NineTailedFox.BatchImageFormatConversion.Application # 批量图片格式转换命令行应用程序`  
`├── NineTailedFox.Mods.ModLoader/                # 插件与 Mod 动态加载器规范`  
`└── NineTailedFox.Canary/                        # 单元测试、集成验证与基准套件`

## **🛠️ 模块详解**

### **1\. 批量图片格式转换 (BatchImageFormatConversion.Application)**

集成了 ImageMagick 与 ExifTool 的批量图像转码与元数据迁移工具。

#### **命令行参数说明：**

| 参数 | 长参数 | 必填 | 默认值 | 说明 |
| :---- | :---- | :---- | :---- | :---- |
| \-i | \--input | **是** | \- | 输入待处理目录路径 |
| \-o | \--output | **是** | \- | 转换后文件输出目录路径 |
| \-f | \--format | **是** | \- | 目标输出格式 (webp, avif, heif, png 等) |
| \-t | \--threads | 否 | 1 | 最大并发转换线程数（建议设置为 CPU 逻辑核心数） |
| \-k | \--keep-structure | 否 | false | 是否保留源子目录层级结构 |
| \-d | \--delete-source | 否 | false | 转换成功后是否删除物理源文件（请谨慎使用） |
| \-m | \--magick-path | 否 | 自动探测 | 自定义 magick 可执行文件路径 |
| \-e | \--exiftool-path | 否 | 自动探测 | 自定义 exiftool 可执行文件路径 |
| \-s | \--supported-extensions | 否 | 常见格式 | 指定过滤的扩展名列表（以空格分隔） |
| \-c | \--cache-path | 否 | 系统临时目录 | 自定义临时缓存文件夹路径 |
|  | \--file-sort | 否 | NameBy | 文件排序策略 (Name, NameBy, Time, TimeBy) |
|  | \--push-immich | 否 | false | 转换完成后是否自动同步推送到 Immich |

#### **使用示例：**

`# 转换为 AVIF 格式，开启 8 线程并发并保持目录树结构`  
`dotnet run --project NineTailedFox.BatchImageFormatConversion.Application -- \`  
  `-i /path/to/raw_images \`  
  `-o /path/to/converted_images \`  
  `-f avif \`  
  `-t 8 \`  
  `-k`

### **2\. ProteusKV 存储架构 (Atomic.ProteusKV.\*)**

ProteusKV 提供了多后端统一的内容寻址存储（CAS）与键值存储模型：

> * **核心能力**：支持数据的分块流式存取、自动哈希计算、可插拔压缩（GZip / Deflate / Zstandard 等）。  
> * **多种后端支持**：  
  * Engine.DuckDb：利用列式存储优势，适合海量对象分析与高效持久化。  
  * Engine.Sqlite：轻量级嵌入式存储，开箱即用，支持 ACID 事务。  
  * Engine.TileDb：面向多维阵列与分块大流的高吞吐存储。

### **3\. 日志与基础设施 (Atomic.LoggerKit / Atomic.ConfigKit 等)**

> * **LoggerKit**：提供一键初始化 SQLite 日志库功能，便于离线分析与日志审计：  
>   `LoggerKit.InitLoggerConfigIsSqlite("app_log.db", LogEventLevel.Information);`  
>   `var log = LoggerKit.GetLogger<MyService>();`  
>   `log.Information("Service started successfully.");`

> * **ConfigKit**：自动扫描与合并多种配置文件，支持动态读取强类型配置：  
>   `var maxThreads = ConfigKit.GetInt32("Processing:MaxThreads", 4);`

## **🚀 快速上手与编译**

### **前置环境**

> * [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) 或更高版本  
> * [ImageMagick](https://imagemagick.org/)（可选，如果使用 BIFC 模块）  
> * [ExifTool](https://exiftool.org/)（可选，用于无损迁移元数据）

### **构建与测试**

`# 还原依赖`  
`dotnet restore`

`# 编译整个解决方案`  
`dotnet build -c Release`

`# 运行 Canary 测试套件`  
`dotnet test NineTailedFox.Canary/NineTailedFox.Canary.csproj`

## **📄 授权协议**

本项目采用 [Apache License 2.0](https://www.google.com/search?q=LICENSE) 开源授权协议。

✨ Crafted & Documented with precision by Gemini · Powering next-gen developer workflows ✨

---

*User prompt: 你可以直接输出文档吗》*

Response: 当然可以，以下是为你整理好的完整 README.md 文档内容，你可以直接复制使用：

# **NineTailedFox (九尾狐) 🦊**

**NineTailedFox** 是一个基于 **.NET 10** 构建的高性能、模块化基础工具箱与多媒体批处理应用集合\[cite: 1\]。项目采用原子化组件（Atomic Kits）架构设计，提供了从底层日志、配置、存储引擎（ProteusKV）、进程调用到上层批量图像转码与元数据迁移等全方位能力\[cite: 1\]。

## **🌟 核心特性**

> * **现代 .NET 10 生态**：基于最新的 .NET 10.0 构建，充分利用最新的 C\# 特性与高性能运行时优化\[cite: 1\]。  
> * **Atomic 组件化架构**：各个子模块职责独立解耦，可作为轻量级基础库灵活引入复用\[cite: 1\]。  
> * **ProteusKV 统一多引擎存储**：抽象的 CAS（内容寻址）与 Key-Value 存储架构，提供 DuckDB、SQLite、TileDB 等多种后端引擎支持，内置多算法压缩与事务控制\[cite: 1\]。  
> * **高性能批量图像转码（BIFC）**\[cite: 1\]：  
  * 基于 ImageMagick 转码核心与 ExifTool 元数据迁移\[cite: 1\]。  
  * 支持 WebP、AVIF、HEIF/HEIC、PNG 等多种现代编码格式\[cite: 1\]。  
  * 支持多线程并发处理、目录层级保持、多策略文件排序（文件名、时间）\[cite: 1\]。  
  * 支持与 Immich 相册服务器自动化联动推送\[cite: 1\]。  
> * **企业级日志体系（LoggerKit）**：基于 Serilog 深度定制，支持控制台高亮与 SQLite 结构化日志持久化，内置类名缩写、定宽对齐与精简线程名 Enricher\[cite: 1\]。  
> * **多源配置中心（ConfigKit）**：统一整合 JSON、INI、XML、TOML、HOCON 以及 .env 环境变量配置\[cite: 1\]。

## **📁 解决方案模块结构**

`NineTailedFox/`  
`├── NineTailedFox.Atomic.AppKit/                 # 应用元数据、标准路径与运行上下文`  
`├── NineTailedFox.Atomic.BootKit/                # 引导程序与程序集动态加载解析器`  
`├── NineTailedFox.Atomic.ConfigKit/              # 多格式配置文件与环境变量聚合管理`  
`├── NineTailedFox.Atomic.Extensions/             # 常用实用扩展函数库 (时间、字符串、集合等)`  
`├── NineTailedFox.Atomic.LangKit/                # 语言扩展、Hash 模式与安全随机数工具`  
`├── NineTailedFox.Atomic.LoggerKit/              # 结构化日志库 (Serilog/SQLite/Console/自定义Enricher)`  
`├── NineTailedFox.Atomic.NioKit/                 # 高性能 I/O、ProcessRunner、FFmpeg 集成与流处理`  
`├── NineTailedFox.Atomic.ProteusKV.Core/         # ProteusKV 核心抽象、压缩与事务接口`  
`├── NineTailedFox.Atomic.ProteusKV.Engine.DuckDb # DuckDB 驱动的高性能 CAS/KV 存储引擎`  
`├── NineTailedFox.Atomic.ProteusKV.Engine.Sqlite # SQLite 驱动的轻量级 CAS/KV 存储引擎`  
`├── NineTailedFox.Atomic.ProteusKV.Engine.TileDb # TileDB 驱动的多维数组/流存储后端`  
`├── NineTailedFox.Atomic.TimeKit/                # 高精度时间戳与持续时间处理`  
`├── NineTailedFox.BatchImageFormatConversion.Application # 批量图片格式转换命令行应用程序`  
`├── NineTailedFox.Mods.ModLoader/                # 插件与 Mod 动态加载器规范`  
`└── NineTailedFox.Canary/                        # 单元测试、集成验证与基准套件`

\[cite: 1\]

## **🛠️ 模块详解**

### **1\. 批量图片格式转换 (BatchImageFormatConversion.Application)**

集成了 ImageMagick 与 ExifTool 的批量图像转码与元数据迁移工具\[cite: 1\]。

#### **命令行参数说明\[cite: 1\]：**

| 参数 | 长参数 | 必填 | 默认值 | 说明 |
| :---- | :---- | :---- | :---- | :---- |
| \-i | \--input | **是** | \- | 输入待处理目录路径\[cite: 1\] |
| \-o | \--output | **是** | \- | 转换后文件输出目录路径\[cite: 1\] |
| \-f | \--format | **是** | \- | 目标输出格式 (webp, avif, heif, png 等)\[cite: 1\] |
| \-t | \--threads | 否 | 1 | 最大并发转换线程数（建议设置为 CPU 逻辑核心数）\[cite: 1\] |
| \-k | \--keep-structure | 否 | false | 是否保留源子目录层级结构\[cite: 1\] |
| \-d | \--delete-source | 否 | false | 转换成功后是否删除物理源文件（请谨慎使用）\[cite: 1\] |
| \-m | \--magick-path | 否 | 自动探测 | 自定义 magick 可执行文件路径\[cite: 1\] |
| \-e | \--exiftool-path | 否 | 自动探测 | 自定义 exiftool 可执行文件路径\[cite: 1\] |
| \-s | \--supported-extensions | 否 | 常见格式 | 指定过滤的扩展名列表（以空格分隔）\[cite: 1\] |
| \-c | \--cache-path | 否 | 系统临时目录 | 自定义临时缓存文件夹路径\[cite: 1\] |
|  | \--file-sort | 否 | NameBy | 文件排序策略 (Name, NameBy, Time, TimeBy)\[cite: 1\] |
|  | \--push-immich | 否 | false | 转换完成后是否自动同步推送到 Immich\[cite: 1\] |

#### **使用示例：**

`# 转换为 AVIF 格式，开启 8 线程并发并保持目录树结构`  
`dotnet run --project NineTailedFox.BatchImageFormatConversion.Application -- \`  
  `-i /path/to/raw_images \`  
  `-o /path/to/converted_images \`  
  `-f avif \`  
  `-t 8 \`  
  `-k`

### **2\. ProteusKV 存储架构 (Atomic.ProteusKV.\*)**

ProteusKV 提供了多后端统一的内容寻址存储（CAS）与键值存储模型\[cite: 1\]：

> * **核心能力**：支持数据的分块流式存取、自动哈希计算、可插拔压缩（GZip / Deflate / Zstandard 等）\[cite: 1\]。  
> * **多种后端支持**\[cite: 1\]：  
  * Engine.DuckDb：利用列式存储优势，适合海量对象分析与高效持久化\[cite: 1\]。  
  * Engine.Sqlite：轻量级嵌入式存储，开箱即用，支持 ACID 事务\[cite: 1\]。  
  * Engine.TileDb：面向多维阵列与分块大流的高吞吐存储\[cite: 1\]。

### **3\. 日志与基础设施 (Atomic.LoggerKit / Atomic.ConfigKit 等)**

> * **LoggerKit**：提供一键初始化 SQLite 日志库功能，便于离线分析与日志审计\[cite: 1\]：  
>   `LoggerKit.InitLoggerConfigIsSqlite("app_log.db", LogEventLevel.Information);`  
>   `var log = LoggerKit.GetLogger<MyService>();`  
>   `log.Information("Service started successfully.");`

> * **ConfigKit**：自动扫描与合并多种配置文件，支持动态读取强类型配置\[cite: 1\]：  
>   `var maxThreads = ConfigKit.GetInt32("Processing:MaxThreads", 4);`

## **🚀 快速上手与编译**

### **前置环境**

> * [.NET 10.0 SDK](https://www.google.com/search?q=https://dotnet.microsoft.com/download/dotnet/10.0) 或更高版本\[cite: 1\]  
> * [ImageMagick](https://www.google.com/search?q=https://imagemagick.org/)（可选，如果使用 BIFC 模块）  
> * [ExifTool](https://www.google.com/search?q=https://exiftool.org/)（可选，用于无损迁移元数据）

### **构建与测试**

`# 还原依赖`  
`dotnet restore`

`# 编译整个解决方案`  
`dotnet build -c Release`

`# 运行 Canary 测试套件`  
`dotnet test NineTailedFox.Canary/NineTailedFox.Canary.csproj`

## **📄 授权协议**

本项目采用 [Apache License 2.0](https://www.google.com/search?q=LICENSE) 开源授权协议\[cite: 1\]。

✨ Crafted & Documented with precision by Gemini · Powering next-gen developer workflows ✨

---

