"""生成 3 份"呼吸阵型"轨迹 JSON，基于原始 unity_cycle.json"""
import json, math, os

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(SCRIPT_DIR, "unity_cycle.json")

with open(SRC, "r") as f:
    data = json.load(f)

cycle = data["cycle_duration_sec"]

# Arm1: 原始轨迹不变
arm1 = json.loads(json.dumps(data))
arm1["planning_group"] = "arm1_breathing"

# Arm2: Joint1 (index 0) 加 Sine 偏移
arm2 = json.loads(json.dumps(data))
arm2["planning_group"] = "arm2_breathing"
for pt in arm2["points"]:
    t = pt["t"]
    offset = 0.15 * math.sin(2 * math.pi * t / cycle)
    pt["positions_rad"][0] += offset

# Arm3: Joint1 加反相 Sine 偏移
arm3 = json.loads(json.dumps(data))
arm3["planning_group"] = "arm3_breathing"
for pt in arm3["points"]:
    t = pt["t"]
    offset = 0.15 * math.sin(2 * math.pi * t / cycle + math.pi)
    pt["positions_rad"][0] += offset

for name, obj in [
    ("breathing_arm1.json", arm1),
    ("breathing_arm2.json", arm2),
    ("breathing_arm3.json", arm3),
]:
    path = os.path.join(SCRIPT_DIR, name)
    with open(path, "w") as f:
        json.dump(obj, f, indent=2)
    print(f"Generated: {path}")

print("Done!")