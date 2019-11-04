# coding=UTF-8
import random
import math
import csv
from collections import Counter, OrderedDict
from operator import itemgetter

### EDIT: WORLD
MONSTER_PARTS_DROP_RATE = 0.03
STAGES_PER_WORLD = 150
STAGES_PER_MONSTER_LV = 5
BASE_EXP = 50
ADDITIONAL_EXP_PER_STAGE = 8
MIN_WAVES = 4
MAX_WAVES = 8
CSV_OUTPUT_FOLDER_PATH = "nekoyume/Assets/AddressableAssets/TableCSV"
WAVE_CSV_NAME = "waves.csv"


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
    "id","wave",
    "monster1_id","monster1_level","monster1_count",
    "monster2_id","monster2_level","monster2_count",
    "monster3_id","monster3_level","monster3_count",
    "monster4_id","monster4_level","monster4_count",
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

def expand_wave_id(wid):
    return "20" + wid[0] + "0" + wid[1:]


## CSV SETUP ##
stage_file = open("StageSheet.csv", "w")
reward_file = open("stage_reward.csv", "w")
stage_csv = csv.writer(stage_file)
reward_csv = csv.writer(reward_file)
stage_csv.writerow(STAGE_FIELD_NAMES)
reward_csv.writerow(REWARD_FIELD_NAMES)

wave_csv = csv.reader(open("WaveTable/sheet-waves.csv", "r"))
stage_to_wave_csv = csv.reader(open("WaveTable/sheet-stage_to_waves.csv", "r"))

wave_dict = {}
stage_dict = {}

for idx, row in enumerate(wave_csv):
    row_id = row[0]
    monsters = sorted(map(expand_wave_id, filter(lambda x: x, row[1:])))
    if idx > 0:
        wave_dict[row_id] = monsters

for idx, row in enumerate(stage_to_wave_csv):
    row_id = row[0]
    waves = filter(lambda x: x, row[1:])
    if idx > 0:
        stage_dict[int(row_id)] = waves

## LOGIC ##
reward_id = 0
for stage in range(1, STAGES_PER_WORLD+1):
    waves = stage_dict[stage]
    exp = BASE_EXP + ADDITIONAL_EXP_PER_STAGE * stage
    # monster level increases 1 per 3 stages
    m_level = int((stage - 1) / STAGES_PER_MONSTER_LV) + 1
    #dstage = (stage-1) % int(STAGES_PER_MONSTER_LV) # between 0 and 2

    monster_parts = []
    # Append each wave
    # print waves
    is_boss_level = (stage % 5 == 0)
    for wave_idx, wave in enumerate(waves):
        is_last_wave = (wave_idx == len(waves) - 1)
        is_boss_wave = int(bool(is_last_wave and is_boss_level))

        monster_picked = wave_dict[wave]
        monster_count = Counter(monster_picked)
        monster_ids = sorted(monster_count.keys())

        # add 3 to 5 monsters
        # add row
        row = [stage, wave_idx+1]
        for i in range(0, 4):
            # sort monsters by count
            if (i < len(monster_ids)):
                mid = monster_ids[i]
                #row += [mid, level, '', monster_count[mid]]
                row += [mid, m_level, monster_count[mid]]
            else:
                row += ['', '', '']
        if is_last_wave: # if last wave, check if boss, add exp and reward
            row += [is_boss_wave, stage, exp]
        else:
            row += [0, 0, 0]
        stage_csv.writerow(row)

    ## setup stage reward
    #reward_row = [((world - 1) * STAGES_PER_WORLD) + stage]
    #parts = Counter(monster_parts)
    ## five rows, prioritize stage specific reward and boss parts reward
    #additional_mat = MATERIAL_LOCATION.get(world, {}).get(stage)
    #if additional_mat:
    #    reward_row += [additional_mat, 1, 1, 2]
    #if boss_id:
    #    reward_row += [MONSTER_PARTS[boss_id], 1, 1, 2]
    ## add rest of them
    #parts_sorted_by_count = OrderedDict(sorted(parts.items(), key=itemgetter(1)))
    #for parts_id in parts_sorted_by_count:
    #    if len(reward_row) < len(REWARD_FIELD_NAMES):
    #        parts_cnt = int(max(math.ceil(MONSTER_PARTS_DROP_RATE * parts_sorted_by_count[parts_id]), 1))
    #        reward_row += [parts_id, 1, parts_cnt, parts_cnt+1]

    #reward_row += [None] * (len(REWARD_FIELD_NAMES) - len(reward_row))
    #reward_csv.writerow(reward_row)
