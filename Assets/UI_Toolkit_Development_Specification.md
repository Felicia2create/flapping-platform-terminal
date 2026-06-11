# UI Toolkit 开发规范 (UXML & USS)

> **版本**: 1.0  
> **适用范围**: Unity UI Toolkit (Unity 2022 LTS+)  
> **核心原则**: UXML ≠ HTML, USS ≠ CSS。UI Toolkit 是 Unity 内置的保留模式 UI 框架，语法与 Web 技术相似但存在关键差异。AI 辅助编码时务必遵循本规范，避免与 HTML/CSS 混淆。

---

## 目录

1. [UXML 规范](#1-uxml-规范)
2. [USS 规范](#2-uss-规范)
3. [UXML ↔ HTML 关键差异对照](#3-uxml--html-关键差异对照)
4. [USS ↔ CSS 关键差异对照](#4-uss--css-关键差异对照)
5. [项目命名约定](#5-项目命名约定)
6. [常见错误与纠正](#6-常见错误与纠正)

---

## 1. UXML 规范

### 1.1 文档结构

```xml
<!-- ✓ 正确：标准 UXML 文档头 -->
<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:uie="UnityEditor.UIElements" display-name="ComponentName">
    <ui:VisualElement name="Root" class="root">
        <!-- 子元素 -->
    </ui:VisualElement>
</ui:UXML>
```

```xml
<!-- ✗ 错误：使用 HTML 文档头 -->
<!DOCTYPE html>
<html>
<head><style>...</style></head>
<body><div>...</div></body>
</html>
```

### 1.2 可用元素列表

**UI Toolkit 内置元素（`ui:` 命名空间，`xmlns:ui="UnityEngine.UIElements"`）：**

| UXML 元素 | 用途 | 对应 C# 类型 |
|-----------|------|-------------|
| `ui:VisualElement` | 通用容器（最常用） | `VisualElement` |
| `ui:Label` | 文本标签 | `Label` |
| `ui:Button` | 按钮 | `Button` |
| `ui:TextField` | 单行文本输入 | `TextField` |
| `ui:FloatField` | 浮点数输入 | `FloatField` |
| `ui:IntegerField` | 整数输入 | `IntegerField` |
| `ui:DoubleField` | 双精度浮点输入 | `DoubleField` |
| `ui:LongField` | 长整数输入 | `LongField` |
| `ui:Slider` | 滑块 | `Slider` |
| `ui:SliderInt` | 整数滑块 | `SliderInt` |
| `ui:Toggle` | 开关/复选框 | `Toggle` |
| `ui:RadioButton` | 单选按钮 | `RadioButton` |
| `ui:RadioButtonGroup` | 单选按钮组 | `RadioButtonGroup` |
| `ui:DropdownField` | 下拉选择 | `DropdownField` |
| `ui:EnumField` | 枚举选择 | `EnumField` |
| `ui:ScrollView` | 滚动区域 | `ScrollView` |
| `ui:ListView` | 列表视图 | `ListView` |
| `ui:TreeView` | 树形视图 | `TreeView` |
| `ui:GroupBox` | 分组框 | `GroupBox` |
| `ui:Image` | 图片 | `Image` |
| `ui:ProgressBar` | 进度条 | `ProgressBar` |
| `ui:Foldout` | 折叠面板 | `Foldout` |
| `ui:PopupWindow` | 弹出窗口 | `PopupWindow` |
| `ui:TabView` | 标签页 | `TabView` |
| `ui:Tab` | 标签页项 | `Tab` |
| `ui:MinMaxSlider` | 范围滑块 | `MinMaxSlider` |
| `ui:Vector2Field` | 二维向量输入 | `Vector2Field` |
| `ui:Vector3Field` | 三维向量输入 | `Vector3Field` |
| `ui:Vector4Field` | 四维向量输入 | `Vector4Field` |
| `ui:RectField` | 矩形输入 | `RectField` |
| `ui:BoundsField` | 边界输入 | `BoundsField` |
| `ui:HelpBox` | 帮助提示框 | `HelpBox` |
| `ui:TemplateContainer` | 模板实例容器 | `TemplateContainer` |
| `ui:Instance` | 模板实例 | `Instance` |
| `ui:Box` | 带边框的容器 | `Box` |

**绝对禁止使用的 HTML 元素：**

```xml
<!-- ✗ 禁止：这些是 HTML 元素，UI Toolkit 中不存在 -->
<div>...</div>
<span>...</span>
<p>...</p>
<h1>~<h6>
<a href="...">
<img src="...">
<input type="...">
<textarea>
<select><option>
<button>
<ul><ol><li>
<table><tr><td><th>
<form>
<section><article><header><footer><nav><aside><main>
<br><hr>
```

### 1.3 元素属性规范

**常用标准属性（所有 VisualElement 通用）：**

| 属性 | 类型 | 说明 |
|------|------|------|
| `name` | string | 元素名称（C# 中通过 `Q<Type>("name")` 查询） |
| `class` | string | USS 样式类（空格分隔多个类名） |
| `tooltip` | string | 鼠标悬停提示 |
| `tabindex` | int | Tab 键焦点顺序 |
| `focusable` | bool | 是否可获取焦点 |
| `visible` | bool | 可见性 |
| `picking-mode` | enum | 点击穿透模式: `Position` / `Ignore` |
| `display` | enum | 显示模式: `Flex` / `None` |

**布局相关属性（直接在 UXML 中设置）：**

```xml
<!-- ✓ 正确：UI Toolkit 支持的布局属性 -->
<ui:VisualElement
    flex-grow="1"
    flex-shrink="0"
    flex-basis="auto"
    width="100px"
    height="60px"
    min-width="50px"
    max-width="300px"
    style="flex-direction: row; align-items: center; justify-content: space-between;"
/>
```

**特定元素的专有属性：**

```xml
<!-- Label 属性 -->
<ui:Label text="显示的文本" />

<!-- Button 属性 -->
<ui:Button text="按钮文字" />

<!-- Slider 属性 -->
<ui:Slider low-value="0" high-value="100" value="50" direction="Horizontal" />

<!-- SliderInt 属性 -->
<ui:SliderInt low-value="0" high-value="100" value="50" direction="Horizontal" />

<!-- TextField 属性 -->
<ui:TextField value="默认文本" max-length="100" is-password="false" multiline="false" />

<!-- FloatField / IntegerField 属性 -->
<ui:FloatField value="0.0" />

<!-- Toggle 属性 -->
<ui:Toggle label="选项文字" value="true" />

<!-- ScrollView 属性 -->
<ui:ScrollView horizontal-scroller-visibility="Auto" vertical-scroller-visibility="Auto" />
```

### 1.4 XML 语法约束

```xml
<!-- ✓ 正确：XML 严格语法 -->
<ui:VisualElement name="Container" class="main-container">
    <!-- 自闭合标签必须有斜杠 -->
    <ui:Image name="Icon" class="icon" />
    <ui:Label text="标题" class="title" />
</ui:VisualElement>

<!-- ✗ 错误：HTML 宽松语法 -->
<ui:VisualElement name="Container" class="main-container">
    <ui:Image name="Icon" class="icon">        <!-- 缺少自闭合斜杠 -->
    <ui:Label text="标题" class="title">        <!-- 缺少自闭合斜杠 -->
</ui:VisualElement>
```

> **关键规则**: UXML 是严格的 XML 文档，所有标签必须正确闭合。非容器元素必须使用自闭合语法 `<Element />`。

### 1.5 模板引用

```xml
<!-- ✓ 正确：引用外部 UXML 模板 -->
<ui:UXML xmlns:ui="UnityEngine.UIElements">
    <ui:Template name="CardTemplate" src="CardTemplate.uxml" />
    <ui:Instance template="CardTemplate" name="Card1" />
</ui:UXML>
```

### 1.6 内联样式

```xml
<!-- ✓ 正确：使用 style 属性设置内联样式（分号分隔） -->
<ui:VisualElement style="flex-grow: 1; background-color: #1a1a2e; margin: 4px; padding: 8px;" />

<!-- ✗ 错误：HTML style 中使用 display: flex -->
<ui:VisualElement style="display: flex; justify-content: center;" />
<!-- UI Toolkit 中 flex 是隐式的，通过 flex-direction 控制 -->
```

---

## 2. USS 规范

### 2.1 文件结构

```css
/* === 文件头部注释 === */
/* 描述该 USS 文件的用途、所属模块 */

/* === 1. CSS 变量定义 === */
:root {
    --color-bg:        #0d1117;
    --color-panel:     #161b22;
    --color-accent:    #58a6ff;
    --font-size:       13px;
    --radius:          4px;
}

/* === 2. 全局/类型选择器 === */
Label {
    color: var(--color-text);
}

/* === 3. 类选择器（按层级排列）=== */
.root { ... }
.top-bar { ... }
.panel-header { ... }

/* === 4. 子元素/嵌套选择器 === */
.top-bar-left { ... }
.top-bar-left .top-bar-title { ... }

/* === 5. 伪类选择器 === */
.action-button:hover { ... }
.action-button:active { ... }
```

### 2.2 USS 支持的属性

#### 2.2.1 颜色与背景

```css
.my-element {
    /* 颜色 */
    color: #c9d1d9;                    /* 文本颜色（无 color: rgb() 写法，建议使用 hex） */
    background-color: #161b22;         /* 背景颜色 */
    background-color: rgba(22, 27, 34, 0.92);  /* 支持 rgba */
    background-color: transparent;     /* 透明（用于穿透到 3D 场景） */

    /* 不支持 background-image / background-size / background-position */
    /* 图片通过 ui:Image 元素显示 */
    /* 不支持 opacity 属性（Unity 2022）—— 可用 rgba 替代 */

    /* 边框 */
    border-width: 1px;                 /* 统一边框宽度 */
    border-left-width: 1px;            /* 单边边框宽度 */
    border-right-width: 1px;
    border-top-width: 1px;
    border-bottom-width: 1px;
    border-color: #30363d;             /* 统一边框颜色 */
    border-left-color: #30363d;        /* 单边边框颜色 */
    border-right-color: #30363d;
    border-top-color: #30363d;
    border-bottom-color: #30363d;
    border-radius: 4px;                /* 圆角 */

    /* ✗ 不支持: border-style, border 简写, outline, box-shadow */
}
```

#### 2.2.2 布局（Flexbox）

```css
.my-element {
    /* === 注意：UI Toolkit 中 Flex 是隐式的，不需要 display: flex === */

    /* 弹性方向 */
    flex-direction: row;               /* 水平排列（默认） */
    flex-direction: column;            /* 垂直排列 */
    flex-direction: row-reverse;       /* 水平反向 */
    flex-direction: column-reverse;    /* 垂直反向 */

    /* 弹性伸缩 */
    flex-grow: 1;                      /* 拉伸系数 */
    flex-shrink: 0;                    /* 收缩系数 */
    flex-basis: auto;                  /* 基准尺寸 */
    flex-wrap: wrap;                   /* 换行: nowrap / wrap / wrap-reverse */

    /* 对齐 */
    align-items: center;               /* 交叉轴对齐: flex-start / flex-end / center / stretch */
    align-self: center;                /* 单项交叉轴对齐 */
    justify-content: space-between;    /* 主轴对齐: flex-start / flex-end / center / space-between / space-around */
    align-content: flex-start;         /* 多行对齐 */

    /* 尺寸 */
    width: 100px;
    height: 60px;
    min-width: 50px;
    max-width: 300px;
    min-height: 20px;
    max-height: 500px;

    /* 溢出 */
    overflow: hidden;                  /* hidden / visible（默认 visible） */
    overflow: auto;                    /* 自动滚动条 */

    /* ✗ 不支持: display (没有 display: flex / block / inline / grid / none) */
    /* ✗ 不支持: CSS Grid 布局 (grid-template-columns, grid-column 等) */
    /* ✗ 不支持: float, clear, position (absolute/relative/fixed), z-index */
    /* ✗ 不支持: box-sizing */
}
```

#### 2.2.3 内边距与外边距

```css
.my-element {
    /* === 不支持简写！必须使用完整属性名 === */

    /* 内边距 */
    padding: 8px;                      /* 四边统一（USS 支持简写 padding） */
    padding-left: 12px;
    padding-right: 12px;
    padding-top: 4px;
    padding-bottom: 4px;

    /* 外边距 */
    margin: 4px;                       /* 四边统一（USS 支持简写 margin） */
    margin-left: 8px;
    margin-right: 8px;
    margin-top: 2px;
    margin-bottom: 2px;

    /* ✗ 不支持: padding: 4px 8px（二值简写） */
    /* ✗ 不支持: padding: 4px 8px 12px 16px（四值简写） */
    /* ✗ 不支持: margin: 4px auto（自动边距） */
}
```

#### 2.2.4 文本样式

```css
.my-element {
    font-size: 13px;                   /* 字号 */
    color: #c9d1d9;                   /* 文字颜色 */

    /* Unity 特有字体样式属性 */
    -unity-font-style: bold;           /* bold / italic / bold-and-italic / normal */
    -unity-font-definition: resource("Fonts/MyFont SDF.asset");  /* 字体资源 */
    -unity-text-align: upper-left;     /* 文本对齐 */
    /* 可选值: upper-left / upper-center / upper-right
              middle-left / middle-center / middle-right
              lower-left / lower-center / lower-right */

    -unity-text-outline-width: 1px;    /* 文字描边宽度 */
    -unity-text-outline-color: #000;   /* 文字描边颜色 */

    /* 文本溢出 */
    -unity-overflow-clip-box: padding-box;  /* overflow 裁剪区域 */
    overflow: hidden;                  /* 隐藏溢出文本（配合 white-space 使用） */
    white-space: nowrap;              /* nowrap / normal */

    /* ✗ 不支持: font-family（通过 -unity-font-definition 设置字体资源） */
    /* ✗ 不支持: font-weight, font-style, line-height, letter-spacing, text-transform */
    /* ✗ 不支持: text-decoration, text-shadow, word-wrap, word-break */

    /* 注意：text 内容不在 USS 中设置，而是在 UXML 的 text 属性或 C# 中设置 */
}
```

#### 2.2.5 伪类选择器

```css
/* USS 支持的伪类 */
.button:hover { background-color: #58a6ff; }
.button:active { opacity: 0.8; }
.button:focus { border-color: #58a6ff; }
.toggle:checked { background-color: #3fb950; }
.element:disabled { opacity: 0.5; }
.element:enabled { opacity: 1.0; }

/* 组合伪类 */
.element:hover:active { ... }

/* ✗ 不支持: :first-child, :last-child, :nth-child, :not(), :before, :after */
/* ✗ 不支持: ::before, ::after 伪元素 */
```

### 2.3 USS 选择器

```css
/* 类型选择器 —— 匹配元素类型 */
Label { color: white; }
Button { border-radius: 4px; }
VisualElement { flex-direction: column; }

/* 类选择器 —— 最常用 */
.top-bar { height: 44px; }
.action-button { background-color: #21262d; }

/* Name 选择器 —— 匹配 name 属性（谨慎使用，优先级高于 class） */
#MainContainer { background-color: #0d1117; }

/* 组合选择器 */
.top-bar .title { font-size: 14px; }        /* 后代选择器 */
.data-section > .section-title { ... }      /* 直接子元素 */
.button.primary { ... }                     /* 多类选择器 */

/* 多选择器并列 */
.left-panel, .right-panel { width: 260px; }

/* ✗ 不支持: 属性选择器 [type="text"], 通用选择器 *, 相邻兄弟 +, 通用兄弟 ~ */
```

### 2.4 USS 变量

```css
/* 定义（:root 中） */
:root {
    --color-bg:     #0d1117;
    --color-accent: #58a6ff;
    --font-size-sm: 11px;
    --font-size:    13px;
    --font-size-lg: 16px;
    --radius:       4px;
    --panel-width:  260px;
}

/* 使用 */
.panel {
    background-color: var(--color-bg);
    width: var(--panel-width);
}

/* 带后备值 */
.element {
    color: var(--custom-color, #c9d1d9);
}
```

### 2.5 不支持的 CSS 属性速查

以下 CSS 常用属性在 UI Toolkit **不存在**，使用时会静默失败或报错：

```
❌ display: flex / block / inline / grid / none
❌ CSS Grid 所有属性 (grid-template-*, grid-column, grid-row, gap)
❌ position: absolute / relative / fixed / sticky
❌ top / right / bottom / left
❌ float / clear
❌ box-sizing
❌ opacity (使用 rgba 替代)
❌ transform (rotate, scale, skew, translate)
❌ transition / animation / @keyframes
❌ z-index
❌ outline
❌ box-shadow
❌ text-shadow
❌ font-family (使用 -unity-font-definition)
❌ font-weight / font-style / line-height
❌ letter-spacing / text-transform
❌ text-decoration
❌ cursor
❌ pointer-events (使用 picking-mode 属性)
❌ background-image / background-size / background-position
❌ border-style (边框样式固定为实线)
❌ :first-child / :last-child / :nth-child()
❌ :not()
❌ ::before / ::after
❌ !important
❌ calc()
❌ clamp() / min() / max()
❌ @media 查询
```

---

## 3. UXML ↔ HTML 关键差异对照

| 概念 | HTML | UXML | 说明 |
|------|------|------|------|
| 根元素 | `<html>` | `<ui:UXML>` | 必须带命名空间声明 |
| 通用容器 | `<div>` | `<ui:VisualElement>` | **没有 div** |
| 行内容器 | `<span>` | `<ui:Label>` | Label 用于短文本 |
| 文本 | `<p>`, `<h1>`~`<h6>` | `<ui:Label>` | 所有文本统一用 Label |
| 按钮 | `<button>` | `<ui:Button>` | PascalCase |
| 输入框 | `<input type="text">` | `<ui:TextField>` | 类型明确 |
| 数字输入 | `<input type="number">` | `<ui:FloatField>` / `<ui:IntegerField>` | 分 int / float |
| 滑块 | `<input type="range">` | `<ui:Slider>` / `<ui:SliderInt>` | 分 int / float |
| 复选框 | `<input type="checkbox">` | `<ui:Toggle>` | 名称不同 |
| 下拉框 | `<select><option>` | `<ui:DropdownField>` | 单元素即可 |
| 图片 | `<img>` | `<ui:Image>` | PascalCase |
| 滚动容器 | `overflow: auto` | `<ui:ScrollView>` | 专用元素 |
| 链接 | `<a href>` | 无原生支持 | 用 Button + C# 逻辑 |
| 唯一标识 | `id` | `name` | C# 中用 `Q<Type>("name")` 查找 |
| 样式类 | `class` | `class` | 语法相同 |
| 内联样式 | `style="..."` | `style="..."` | 语法相似但属性名不同 |
| 注释 | `<!-- -->` | `<!-- -->` | 语法相同 |
| 自闭合 | `<img>`(HTML5) | `<ui:Image />` | XML 严格要求 `/>` |
| 模板 | `<template>` | `<ui:Template>` / `<ui:Instance>` | 加载 .uxml 文件 |

---

## 4. USS ↔ CSS 关键差异对照

| 概念 | CSS (Web) | USS (UI Toolkit) | 说明 |
|------|-----------|------------------|------|
| 布局模式 | `display: flex` | **隐式 flex** | flex-direction 决定，无需 display |
| CSS Grid | ✅ 完整支持 | ❌ **不支持** | 只能用 Flexbox |
| 定位 | `position: relative/absolute/fixed` | ❌ **不支持** | 没有绝对定位 |
| 层级 | `z-index` | ❌ **不支持** | 按 UXML 顺序排列 |
| 透明度 | `opacity: 0.5` | ❌ **不支持** | 使用 `rgba()` 颜色 |
| 过渡动画 | `transition: ...` | ❌ **不支持** | 通过 C# 代码实现 |
| 变形 | `transform: rotate(...)` | ❌ **不支持** | 通过 C# 代码实现 |
| 字体 | `font-family: ...` | `-unity-font-definition: ...` | 使用 Unity 字体资源 |
| 字体粗细 | `font-weight: bold` | `-unity-font-style: bold` | 前缀不同 |
| 文本对齐 | `text-align: center` | `-unity-text-align: middle-center` | 前缀和值都不同 |
| 文本装饰 | `text-decoration: underline` | ❌ **不支持** | |
| 阴影 | `box-shadow` / `text-shadow` | ❌ **不支持** | |
| 盒模型 | `box-sizing: border-box` | ❌ **不支持** | |
| 边框 | `border: 1px solid red` | 分属性写 `border-width`/`border-color` | 无 `border-style` |
| 边距简写 | `margin: 4px 8px` | 仅支持单值 `margin: 4px` | 多值简写不支持 |
| 计算 | `calc(100% - 20px)` | ❌ **不支持** | 用 flex-grow 替代 |
| 媒体查询 | `@media (max-width: ...)` | ❌ **不支持** | |
| 伪元素 | `::before` / `::after` | ❌ **不支持** | |
| 伪类 | `:nth-child()`, `:first-child` | ❌ **不支持** | 仅 `:hover/:active/:focus/:checked/:disabled/:enabled` |
| 变量 | `--var-name` | `--var-name` | ✅ 语法相同（Unity 2022+） |
| `!important` | ✅ 支持 | ❌ **不支持** | 靠选择器优先级 |
| 注释 | `/* */` | `/* */` | ✅ 语法相同 |

---

## 5. 项目命名约定

### 5.1 文件组织

```
Assets/
└── FPT/
    └── UI/
        └── UI_Toolkit/
            ├── UXML/                  # 所有 .uxml 布局文件
            │   ├── MainLayout.uxml
            │   ├── TopBar.uxml
            │   ├── Panels/
            │   │   ├── LeftPanel.uxml
            │   │   └── RightPanel.uxml
            │   └── Widgets/
            │       ├── JointSlider.uxml
            │       └── PoseDisplay.uxml
            └── USS/                   # 所有 .uss 样式文件
                ├── Main.uss           # 全局样式 + 变量
                ├── TopBar.uss
                ├── Panels.uss
                └── Widgets.uss
```

### 5.2 命名规范

| 项目 | 规范 | 示例 |
|------|------|------|
| UXML/BSS 文件名 | PascalCase | `MainLayout.uxml`、`TopBar.uss` |
| 元素 `name` | PascalCase | `name="TopBarLeft"`、`name="JSlider0"` |
| CSS 类名 | kebab-case | `class="top-bar-left"`、`class="action-button"` |
| USS 变量 | kebab-case | `--color-bg`、`--font-size-sm` |

### 5.3 注释规范

```xml
<!-- UXML 注释：使用与 HTML 相同的语法 -->
<!-- 描述该区域的功能用途 -->
<ui:VisualElement name="TopBar" class="top-bar">
    ...
</ui:VisualElement>
```

```css
/* USS 注释：使用 CSS 块注释语法 */
/* === 一级分隔 === */
/* --- 二级分隔 --- */

/* 
 * 多行注释
 * 描述复杂的样式逻辑
 */
```

---

## 6. 常见错误与纠正

### 6.1 使用 HTML 元素名

```xml
<!-- ✗ 错误 -->
<div class="container">
    <span class="title">Hello</span>
    <button class="btn">Click</button>
    <input type="text" placeholder="输入..." />
</div>

<!-- ✓ 正确 -->
<ui:VisualElement class="container">
    <ui:Label text="Hello" class="title" />
    <ui:Button text="Click" class="btn" />
    <ui:TextField class="input" />
</ui:VisualElement>
```

### 6.2 使用 CSS display 属性

```css
/* ✗ 错误 */
.container {
    display: flex;
    flex-direction: column;
}

/* ✓ 正确 */
.container {
    flex-direction: column;  /* flex 是隐式的 */
}
```

### 6.3 使用 CSS Grid

```css
/* ✗ 错误 —— UI Toolkit 不支持 Grid */
.grid-layout {
    display: grid;
    grid-template-columns: 1fr 1fr 1fr;
    gap: 8px;
}

/* ✓ 正确 —— 用嵌套 Flexbox 替代 */
.grid-row {
    flex-direction: row;
}
.grid-row > .cell {
    flex-grow: 1;
    margin: 4px;
}
```

### 6.4 使用 position 定位

```css
/* ✗ 错误 —— UI Toolkit 不支持 position */
.floating-button {
    position: absolute;
    right: 10px;
    bottom: 10px;
}

/* ✓ 正确 —— 用 margin / flex 对齐替代 */
.floating-button {
    align-self: flex-end;
    margin-right: 10px;
    margin-bottom: 10px;
}
```

### 6.5 使用 CSS transition/animation

```css
/* ✗ 错误 —— UI Toolkit 不支持 CSS 动画 */
.button {
    transition: background-color 0.3s;
}
.button:hover {
    background-color: blue;
}

/* ✓ 正确 —— 用伪类直接切换，动画通过 C# 实现 */
.button:hover {
    background-color: var(--color-accent);
}
```

### 6.6 忘记 XML 自闭合

```xml
<!-- ✗ 错误 -->
<ui:Image name="Icon" class="icon">
<ui:Label text="Hello">

<!-- ✓ 正确 -->
<ui:Image name="Icon" class="icon" />
<ui:Label text="Hello" />
```

### 6.7 使用 font-family 设置字体

```css
/* ✗ 错误 */
.title {
    font-family: "SimHei", "Arial", sans-serif;
    font-weight: bold;
}

/* ✓ 正确 */
.title {
    -unity-font-style: bold;
    /* 中文字体通过 -unity-font-definition 设置，或使用 Font Asset */
}
```

### 6.8 使用 CSS border 简写

```css
/* ✗ 错误 */
.panel {
    border: 1px solid #30363d;
}

/* ✓ 正确 */
.panel {
    border-width: 1px;
    border-color: #30363d;
}
```

### 6.9 UXML 中使用 HTML 专有属性

```xml
<!-- ✗ 错误 -->
<ui:TextField placeholder="请输入..." id="input1" />
<ui:Button onclick="handleClick()" disabled="true" />

<!-- ✓ 正确 -->
<ui:TextField name="InputField1" />
<!-- placeholder 和 disabled 在 C# 中设置 -->
<!-- 事件绑定在 C# 中通过 RegisterCallback 实现 -->
```

---

## 附录 A：快速检查清单

在提交 UXML/USS 代码前，逐项检查：

### UXML 检查清单
- [ ] 所有元素使用 `ui:` 命名空间前缀
- [ ] 没有使用 HTML 元素名（div, span, p, h1, input, button 等）
- [ ] 所有非容器元素正确自闭合（`/>`）
- [ ] `name` 属性使用 PascalCase
- [ ] `class` 属性使用 kebab-case
- [ ] 文档声明为 `<ui:UXML xmlns:ui="UnityEngine.UIElements">`

### USS 检查清单
- [ ] 没有使用 `display: flex` 或 `display: grid`
- [ ] 没有使用 `position` / `z-index`
- [ ] 没有使用 `opacity`（用 rgba 替代）
- [ ] 没有使用 `transition` / `animation` / `transform`
- [ ] 没有使用 `font-family` / `font-weight` / `line-height`
- [ ] 没有使用 `border` 简写
- [ ] 没有使用 `box-shadow` / `text-shadow`
- [ ] 没有使用 `calc()` / CSS 函数
- [ ] 字体样式使用 `-unity-font-style`
- [ ] 文本对齐使用 `-unity-text-align`

---

## 附录 B：US 属性速查表

| 布局 | 尺寸 | 间距 | 外观 | 文本 |
|------|------|------|------|------|
| `flex-direction` | `width` | `margin-*` | `background-color` | `font-size` |
| `flex-grow` | `height` | `padding-*` | `border-width` | `color` |
| `flex-shrink` | `min-width` | `margin` | `border-color` | `-unity-font-style` |
| `flex-basis` | `max-width` | `padding` | `border-radius` | `-unity-text-align` |
| `flex-wrap` | `min-height` | | `border-*-width` | `-unity-font-definition` |
| `align-items` | `max-height` | | `border-*-color` | `-unity-text-outline-*` |
| `align-self` | | | | `white-space` |
| `justify-content` | | | | `overflow` |
| `align-content` | | | | `-unity-overflow-clip-box` |

---

> **维护者**: FPT 开发团队  
> **最后更新**: 2026-06-02  
> **关联文档**: Unity UI Toolkit 官方文档 [https://docs.unity3d.com/Manual/UIElements.html](https://docs.unity3d.com/Manual/UIElements.html)
