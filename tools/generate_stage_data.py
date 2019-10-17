import random
import math
import csv
from collections import Counter, OrderedDict
from operator import itemgetter

### EDIT: WORLD
MONSTER_PARTS_DROP_RATE = 0.03
STAGES_PER_WORLD = 50
STAGES_PER_MONSTER_LV = 1
BASE_EXP = 50
ADDITIONAL_EXP_PER_STAGE = 8
MIN_WAVES = 4
MAX_WAVES = 8


### EDIT: MAP DATA ###
# 204000, 204001, 204002, 204003, 204004, 204010, 204011, 204012, 204013, 204014 공용
# 201000, 201001, 201002, 201003, 201004, 201005 똥글이, 꽃들 (W1)
# 202000, 202001, 202002, 202003, 202004, 202005, 202006 여우 (W1)
# 203000, 203001, 203002, 203003, 203004, 203005, 203006 멧돼지 (W2)
# 
WORLD_MONSTERS = {
    1: [204010, 204000, 201000, 204011, 204001, 201001, 204012, 204002, 201002, 202001, 204013, 204003, 201003, 204014, 204004, 201004, 202003, 202004, 202005],
    2: [204010, 204000, 203000, 204011, 204001, 203001, 204012, 204002, 203002, 204013, 204003, 203003, 204014, 204004, 203004, 203005],
    3: [204010, 204000, 205000, 204011, 204001, 203001, 204012, 204002, 203002, 204013, 204003, 205003, 204014, 204004, 203004, 203005],
}

# monster_id -> parts_id
MONSTER_PARTS = {
    201000: 306000,
    201001: 306001,
    201002: 306002,
    201003: 306003,
    201004: 306005,
    201005: 306006,
    202000: 306009,
    202001: 306010,
    202002: 306011,
    202003: 306012,
    202004: 306015,
    202005: 306019,
    202006: 306018,
    202007: 306020,
    203000: 306023,
    203001: 306025,
    203002: 306027,
    203003: 306029,
    203004: 306031,
    203005: 306033,
    203006: 306032,
    203007: 306034,
    204000: 306035,
    204001: 306036,
    204002: 306037,
    204003: 306038,
    204004: 306039,
    204010: 306035,
    204011: 306036,
    204012: 306037,
    204013: 306038,
    204014: 306039,
    205000: 306035,
    205003: 306038
}

# extra material drops
MATERIAL_LOCATION = {
    1: {
        3: 303000,
        5: 302002,
        6: 303100,
        9: 303200,
        10: 302001,
        12: 303300,
        15: 303400,
        18: 303001,
        20: 302000,
        21: 303101,
        24: 303201,
        25: 302003,
        27: 303301,
        30: 303401,
        34: 303102,
        35: 302004,
        38: 303202,
        40: 302005,
        42: 303302,
        44: 303402,
        45: 302006,
        48: 303002,
        50: 302009
    },
    2: {
        9: 303102,
        10: 302007,
        19: 303202,
        20: 302008,
        29: 303302,
        30: 302009,
        39: 303402,
        40: 302009,
        49: 303002,
        50: 302009
    },
    3: {
        9: 303102,
        10: 302000,
        19: 303202,
        20: 302003,
        29: 303302,
        30: 302004,
        39: 303402,
        40: 302005,
        49: 303002,
        50: 302006
    }
}

# world->stage->boss_id
BOSS_LOCATION = {
    1: {
        5:201002,
        10:201003,
        15:201004,
        20:201005,
        25:201002,
        30:202003,
        35:202004,
        40:202005,
        45:202006,
        50:202007
    },
    2: {
        10: 203005,
        20: 203006,
        30: 203006,
        40: 203006,
        50: 203007        
    },
    3: {
        10: 202007,
        20: 203007,
        30: 203007,
        40: 203007,
        50: 203007
    },
}

## OTHER CONSTS ##
#REWARD_BASE = 101000
STAGE_FIELD_NAMES = [
    "id","stage","wave",
    "monster1_id","monster1_level","monster1_visual","monster1_count",
    "monster2_id","monster2_level","monster2_visual","monster2_count",
    "monster3_id","monster3_level","monster3_visual","monster3_count",
    "monster4_id","monster4_level","monster4_visual","monster4_count",
    "is_boss","reward","exp"
]

REWARD_FIELD_NAMES = [
    "id",
    "item1","item1_ratio","item1_min","item1_max",
    "item2","item2_ratio","item2_min","item2_max",
    "item3","item3_ratio","item3_min","item3_max",
    "item4","item4_ratio","item4_min","item4_max",
    "item5","item5_ratio","item5_min","item5_max",
]

## CSV SETUP ##
stage_file = open("stage.csv", "w")
reward_file = open("stage_reward.csv", "w")
stage_csv = csv.writer(stage_file)
reward_csv = csv.writer(reward_file)
stage_csv.writerow(STAGE_FIELD_NAMES)
reward_csv.writerow(REWARD_FIELD_NAMES)

## LOGIC ##
stage_id = 0
reward_id = 0
for world, monster_list in WORLD_MONSTERS.items():
    for stage in range(1, STAGES_PER_WORLD+1):
        # exp per stage
        exp = BASE_EXP + ADDITIONAL_EXP_PER_STAGE * stage
        # monster level increases 1 per 3 stages
        level = int((((world - 1) * STAGES_PER_WORLD) + stage - 1) / STAGES_PER_MONSTER_LV) + 1
        dstage = (stage-1) % int(STAGES_PER_MONSTER_LV) # between 0 and 2
        boss_id = BOSS_LOCATION.get(world,{}).get(stage)
        # number of waves varies from 5 to 8
        if MIN_WAVES == MAX_WAVES:
            waves = MAX_WAVES
        else:
            waves = random.randrange(
                MIN_WAVES,
                int(MAX_WAVES + (MAX_WAVES - MIN_WAVES) * dstage / STAGES_PER_MONSTER_LV))
        monster_parts = []
        for wave in range(1, waves+1):
            is_last_wave = (wave == waves)
            is_boss_wave = int(bool(is_last_wave and boss_id))

            monster_picked = []

            monster_count = random.randrange(3, min(3 + wave, 12))
            if is_boss_wave:
                monster_count = 5
            # add 3 to 5 monsters
            for idx in range(0, monster_count):
                if (idx == 0) and is_boss_wave:
                    monster_picked.append(boss_id)
                else:
                    last_monster = math.ceil(
                        len(monster_list) * (float(stage)/(STAGES_PER_WORLD+1))
                    )
                    random_monster = random.choice(monster_list[max(0, last_monster - 6):last_monster])
                    monster_picked.append(random_monster)
                    monster_parts.append(MONSTER_PARTS[random_monster])

            # sort monsters by count
            monster_count = Counter(monster_picked)
            monster_ids = monster_count.keys()
            monster_ids = sorted(monster_ids)

            stage_id += 1
            # add row
            row = [stage_id, ((world - 1) * STAGES_PER_WORLD) + stage, wave]
            for i in range(0, 4):
                if (i < len(monster_ids)):
                    mid = monster_ids[i]
                    row += [mid, level + (1 if is_boss_wave else 0), '', monster_count[mid]]
                else:
                    row += ['', '', '', '']
            if is_last_wave: # if last wave, check if boss, add exp and reward
                row += [is_boss_wave, ((world - 1) * STAGES_PER_WORLD) + stage, exp]
            else:
                row += [0, 0, 0]
            stage_csv.writerow(row)

        # setup stage reward
        reward_row = [((world - 1) * STAGES_PER_WORLD) + stage]
        parts = Counter(monster_parts)
        # five rows, prioritize stage specific reward and boss parts reward
        additional_mat = MATERIAL_LOCATION.get(world, {}).get(stage)
        if additional_mat:
            reward_row += [additional_mat, 1, 1, 2]
        if boss_id:
            reward_row += [MONSTER_PARTS[boss_id], 1, 1, 2]
        # add rest of them
        parts_sorted_by_count = OrderedDict(sorted(parts.items(), key=itemgetter(1)))
        for parts_id in parts_sorted_by_count:
            if len(reward_row) < len(REWARD_FIELD_NAMES):
                parts_cnt = int(max(math.ceil(MONSTER_PARTS_DROP_RATE * parts_sorted_by_count[parts_id]), 1))
                reward_row += [parts_id, 1, parts_cnt, parts_cnt+1]

        reward_row += [None] * (len(REWARD_FIELD_NAMES) - len(reward_row))
        reward_csv.writerow(reward_row)
