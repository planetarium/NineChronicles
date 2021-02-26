# coding=UTF-8
import random
import math
import csv
from collections import Counter, OrderedDict
from operator import itemgetter

### EDIT: WORLD
MONSTER_PARTS_DROP_RATE = 0.3
#STAGES_PER_WORLD = 150
STAGES_PER_MONSTER_LV = 4
BASE_EXP = 50
ADDITIONAL_EXP_PER_STAGE = 8
MIN_WAVES = 4
MAX_WAVES = 8
CSV_OUTPUT_FOLDER_PATH = "nekoyume/Assets/AddressableAssets/TableCSV"
WAVE_CSV_NAME = "waves.csv"

WAVE_BASE = {
    "low": [4000, 4001, 4002, 4005, 4006, 4007, 4010, 4011, 4012, 4015, 4016, 4017],
    "mid": [4100, 4101, 4102, 4110, 4111],
    "high": [4200, 4201, 4202]
}

WAVE_WORLD_1 = {
    "low": range(1000, 1010),
    "mid": range(1100, 1108),
    "high": range(1200, 1203),
}

WAVE_WORLD_2 = {
    "low": range(2000, 2011),
    "mid": range(2100, 2104),
    "high": range(2200, 2201),
}

WAVE_WORLD_3 = {
    "low": [],
    "mid": [],
    "high": [],
}

WAVE_WORLD_4 = {
    "low": [],
    "mid": [],
    "high": [],
}


# monster_id -> parts_id
MONSTER_PARTS = {
    '201000': 306000,
    '201001': 306001,
    '201002': 306002,
    '201003': 306003,
    '201004': 306005,
    '201005': 306006,
    '202000': 306009,
    '202001': 306010,
    '202002': 306011,
    '202003': 306012,
    '202004': 306015,
    '202005': 306019,
    '202006': 306018,
    '202007': 306020,
    '203000': 306023, # 나무둔기 atk
    '203001': 306025, # 맷돼지뿔 DEF
    '203002': 306027, # 둔기조각 CRI
    '203003': 306029, # 맷돼지뿔 HP
    '203004': 306031, # 쇠사슬 DOG
    '203005': 306033, # 제림니르 도끼 ATK
    '203006': 306032, # 부러진 지팡이 HP
    '203007': 306034, # 제림니르 철퇴 SPD
    '204000': 306035,
    '204001': 306036,
    '204002': 306037,
    '204003': 306038,
    '204004': 306039,
    '204010': 306040,
    '204011': 306041,
    '204012': 306042,
    '204013': 306043,
    '204014': 306044,
    '204020': 306045,
    '204021': 306046,
    '204022': 306047,
    '204023': 306048,
    '204024': 306049,
    '205000': 306050,
    '205003': 306051,
    '205007': 306054
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
    "item6","item6_ratio","item6_min","item6_max",
    "item7","item7_ratio","item7_min","item7_max",
    "item8","item8_ratio","item8_min","item8_max",
    "item9","item9_ratio","item9_min","item9_max",
    "item10","item10_ratio","item10_min","item10_max",
]

def expand_wave_id(wid):
    return "20" + wid[0] + "0" + wid[1:]


# TODO food grade
FOOD_MATERIAL = [302000, 302000, 302000, 302001, 302001, 302002, 302002, 302003, 302003, 302004,
        302005, 302005, 302006, 302007, 302007, 302007, 302008, 302008, 302009]
FOOD_DROP_RATE = 0.15;

EQ_MAT_G1 = [303000, 303100, 303200, 303300, 303400]
EQ_G1_DROP_RATE = 0.25;
EQ_MAT_G2 = [303001, 303101, 303201, 303301, 303401]
EQ_G2_DROP_RATE = 0.125;
EQ_MAT_G3 = [303002, 303102, 303202, 303302, 303402]
EQ_G3_DROP_RATE = 0.05;
EQ_MAT_G4 = [303003, 303103, 303203, 303303, 303403]
EQ_G4_DROP_RATE = 0.025;
EQ_MAT_G5 = [303004, 303104, 303204, 303304, 303404]
EQ_G5_DROP_RATE = 0.01;

CHALLENGE_DROP = [301000, 304000, 304001, 304002, 304003]
CHALLENGE_DROP_RATE = 0.05;



## CSV SETUP ##
stage_file = open("StageSheet.csv", "w")
reward_file = open("StageRewardSheet.csv", "w")
stage_csv = csv.writer(stage_file)
reward_csv = csv.writer(reward_file)
stage_csv.writerow(STAGE_FIELD_NAMES)
reward_csv.writerow(REWARD_FIELD_NAMES)

wave_csv = csv.reader(open("WaveTable/sheet-waves.csv", "r"))
stage_to_wave_csv = csv.reader(open("WaveTable/sheet-stage_to_waves.csv", "r"))

wave_dict = {}
stage_dict = {}
stage_ids = []

for idx, row in enumerate(wave_csv):
    row_id = row[0]
    monsters = sorted(map(expand_wave_id, filter(lambda x: x, row[1:])))
    if idx > 0:
        wave_dict[row_id] = monsters

for idx, row in enumerate(stage_to_wave_csv):
    if idx > 0:
        row_id = row[0]
        base_lv = int(row[1])
        wave_cnt = int(row[2])
        wave1 = row[3]
        wave2 = row[4]
        wave3 = row[5]
        boss_wave = row[6]
        last_wave = boss_wave or wave3 or wave2
        waves = filter(lambda wave: wave, [wave1, wave2, wave2, wave3])

        grouped = []
        for wave_idx in range(0, wave_cnt):
            is_last_wave = wave_idx == wave_cnt - 1
            random_lv = base_lv
            if random.random < 0.4 and random_lv > 1:
                random_lv -= 1

            if is_last_wave:
                grouped.append([last_wave, base_lv])
            elif wave_idx is 0:
                grouped.append([wave1, max(1, base_lv - 1)])
            elif wave_idx is 1:
                grouped.append([wave2, random_lv])
            else:
                grouped.append([random.choice(waves), random_lv])

        #grouped = filter(lambda row: row[0] , [waves[i:i+2] for i in range(0, len(waves), 2)])

        stage_dict[int(row_id)] = grouped
        stage_ids.append(int(row_id))


## LOGIC ##
reward_id = 0
for stage in stage_ids:
    world = int((stage-1) / 50)
    waves = stage_dict[stage]
    exp = 0
    stage_idx = (stage-1) % 50
    if (world < 3):
        exp = int((BASE_EXP + ADDITIONAL_EXP_PER_STAGE * stage_idx) * (world + 1.5)/1.5)

    #monster_parts = []
    random_monster = None
    last_monster = None
    # Append each wave
    # print waves
    is_boss_level = (stage % 10 == 0)
    wave_cnt = len(waves)
    for wave_idx, wave_info in enumerate(waves):
        print wave_info
        wave = wave_info[0]
        m_level = wave_info[1]
        is_last_wave = (wave_idx == wave_cnt - 1)
        is_boss_wave = int(bool(is_last_wave and is_boss_level))
        if int(stage) > 1000:
            is_boss_wave = 1

        if wave in wave_dict:
            monster_picked = wave_dict[wave]
            monster_count = Counter(monster_picked)
            monster_ids = sorted(monster_count.keys())

            # add 3 to monsters
            # add row
            row = [stage, wave_idx+1]
            for i in range(0, 4):
                # sort monsters by count
                if (i < len(monster_ids)):
                    mid = monster_ids[i]
                    #row += [mid, level, '', monster_count[mid]]
                    row += [mid, m_level, monster_count[mid]]
                    last_monster = mid
                    if (random_monster is None) or ((random.random() > 0.5) and (wave_idx is 0)):
                        random_monster = mid
                else:
                    row += ['', '', '']
            if is_last_wave: # if last wave, check if boss, add exp and reward
                row += [is_boss_wave, stage, exp]
            else:
                row += [0, 0, 0]
            stage_csv.writerow(row)

    # setup stage reward
    reward_row = [stage]
    # five rows, prioritize stage specific reward and boss parts reward

    food_mat = random.choice(FOOD_MATERIAL)
    g1_mat = random.choice(EQ_MAT_G1)
    g2_mat = random.choice(EQ_MAT_G2)
    g3_mat = random.choice(EQ_MAT_G3)
    g4_mat = random.choice(EQ_MAT_G4)
    g5_mat = random.choice(EQ_MAT_G5)

    if stage < 151:
        reward_row += [MONSTER_PARTS[random_monster], MONSTER_PARTS_DROP_RATE, 1, max(1, int(stage_idx/8))]
        reward_row += [MONSTER_PARTS[last_monster], MONSTER_PARTS_DROP_RATE/2.0, 1, max(1, int(stage_idx/12))]
        reward_row += [food_mat, FOOD_DROP_RATE, 1, 1]

    if stage is 2:
        reward_row += [303000, 0.75, 1, 1]
    elif stage < 11:
        reward_row += [g1_mat, 0.25, 1, 1]
        g1_mat_2 = random.choice(EQ_MAT_G1)
        reward_row += [g1_mat_2, 0.10, 1, 1]
    elif stage < 31:
        reward_row += [g1_mat, 0.20, 1, 1]
        reward_row += [g2_mat, 0.05, 1, 1]
    elif stage < 51:
        reward_row += [g1_mat, 0.15, 1, 1]
        reward_row += [g2_mat, 0.08, 1, 1]
        reward_row += [g3_mat, 0.02, 1, 1]
    elif stage < 61:
        reward_row += [g1_mat, 0.15, 1, 1]
        reward_row += [g2_mat, 0.10, 1, 1]
    elif stage < 81:
        reward_row += [g1_mat, 0.10, 1, 1]
        reward_row += [g2_mat, 0.10, 1, 1]
        reward_row += [g3_mat, 0.05, 1, 1]
        reward_row += [g4_mat, 0.01, 1, 1]
    elif stage < 101:
        reward_row += [g1_mat, 0.05, 1, 1]
        reward_row += [g2_mat, 0.15, 1, 1]
        reward_row += [g3_mat, 0.04, 1, 1]
    elif stage < 111:
        reward_row += [g2_mat, 0.15, 1, 1]
        reward_row += [g3_mat, 0.03, 1, 1]
    elif stage < 131:
        reward_row += [g2_mat, 0.10, 1, 1]
        reward_row += [g3_mat, 0.11, 1, 1]
        reward_row += [g4_mat, 0.02, 1, 1]
    elif stage < 151:
        reward_row += [g2_mat, 0.05, 1, 1]
        reward_row += [g3_mat, 0.15, 1, 1]
        reward_row += [g4_mat, 0.04, 1, 1]
        reward_row += [g5_mat, 0.01, 1, 1]
    else:
        # final stages, wrong reward
        ch_mat = random.choice(CHALLENGE_DROP)
        ch_mat_2 = random.choice(CHALLENGE_DROP)
        reward_row += [ch_mat, CHALLENGE_DROP_RATE, 1, 1]
        reward_row += [ch_mat, CHALLENGE_DROP_RATE, 1, 1]

    while len(reward_row) < 41:
        reward_row += ['','','',''] # add five empty rows

#    if additional_mat:
#        reward_row += [additional_mat, 1, 1, 2]
#    if boss_id:
#        reward_row += [MONSTER_PARTS[boss_id], 1, 1, 2]
#    # add rest of them
#    parts_sorted_by_count = OrderedDict(sorted(parts.items(), key=itemgetter(1)))
#    for parts_id in parts_sorted_by_count:
#        if len(reward_row) < len(REWARD_FIELD_NAMES):
#            parts_cnt = int(max(math.ceil(MONSTER_PARTS_DROP_RATE * parts_sorted_by_count[parts_id]), 1))
#            reward_row += [parts_id, 1, parts_cnt, parts_cnt+1]

    reward_csv.writerow(reward_row)
