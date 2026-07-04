"""生成 3 份"聚散绽放"轨迹 JSON，基于原始 unity_cycle.json

策略：
  3 臂安装间隔 120°，各自本地坐标系下 Joint1 (Yaw) 同相偏移
  → 视觉上三角形同时收缩/展开（聚散效果）。
  Joint2 (Pitch) 加 90° 相位差 cos 偏移
  → 聚拢时微仰、散开时微俯，形成"花朵绽放"感。

  arm1 = 基准 + 聚散偏移（与 arm2/arm3 相同偏移量）
  arm2 = 基准 + 聚散偏移（同 arm1）
  arm3 = 基准 + 聚散偏移（同 arm1）
"""
import json, math, os

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(SCRIPT_DIR, "unity_cycle.json")

with open(SRC, "r") as f:
    data = json.load(f)

cycle = data["cycle_duration_sec"]

# ── 聚散绽放参数 ──
A_YAW   = 0.25   # Joint1 (Yaw) 水平聚散振幅，rad
A_PITCH = 0.15   # Joint2 (Pitch) 俯仰绽放振幅，rad

def apply_bloom(src, group_name):
    """对一份轨迹数据施加聚散绽放偏移，返回新数据。"""
    obj = json.loads(json.dumps(src))
    obj["planning_group"] = group_name
    for pt in obj["points"]:
        t = pt["t"]
        # Joint1 (index 0): Yaw 水平聚散，同相 sin
        pt["positions_rad"][0] += A_YAW * math.sin(2 * math.pi * t / cycle)
        # Joint2 (index 1): Pitch 俯仰绽放，cos（相位差 90°）
        pt["positions_rad"][1] += A_PITCH * math.cos(2 * math.pi * t / cycle)
    return obj

arm1 = apply_bloom(data, "arm1_breathing")
arm2 = apply_bloom(data, "arm2_breathing")
arm3 = apply_bloom(data, "arm3_breathing")

for name, obj in [
    ("breathing_arm1.json", arm1),
    ("breathing_arm2.json", arm2),
    ("breathing_arm3.json", arm3),
]:
    path = os.path.join(SCRIPT_DIR, name)
    with open(path, "w") as f:
        json.dump(obj, f, indent=2)
    print(f"Generated: {path}")

print(f"Done! A_YAW={A_YAW}, A_PITCH={A_PITCH}, cycle={cycle:.3f}s")