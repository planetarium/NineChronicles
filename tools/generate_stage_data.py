import random
import math
import csv
from collections import Counter, OrderedDict
from operator import itemgetter

### EDIT: WORLD
MONSTER_PARTS_DROP_RATE = 0.15
STAGES_PER_WORLD = 50
STAGES_PER_MONSTER_LV = 3
BASE_EXP = 30
ADDITIONAL_EXP_PER_STAGE = 5
MIN_WAVES = 5
MAX_WAVES = 10


### EDIT: MAP DATA ###
WORLD_MONSTERS = {
    1: [201000, 201001, 201002, 201003, 202000, 202001, 201000, 201001, 201002, 201003, 201000, 201001],
    2: [201000, 201001, 201002, 201003, 202000, 202001, 201000, 201001, 201002, 201003, 201000, 201001],
    3: [201000, 201001, 201002, 201003, 202000, 202001, 201000, 201001, 201002, 201003, 201000, 201001],
}

# monster_id -> parts_id
MONSTER_PARTS = {
    201000: 234,
    201001: 234,
    201002: 234,
    201003: 234,
    202000: 234,
    202001: 234,
    202020: 234,
}

# extra material drops
MATERIAL_LOCATION = {
    1: {
        2: 300003 # world 1, stage 1, material 300003
    },
    2: {
        5: 300003
    },
    3: {
        5: 300003
    }
}

# world->stage->boss_id
BOSS_LOCATION = {
    1: {
        10: 202020
    },
    2: {
        10: 202020
    },
    3: {
        10: 202020
    },
}

## OTHER CONSTS ##
#REWARD_BASE = 101000
STAGE_FIELD_NAMES = [
    "id","world","stage","wave",
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
stage_file = open("stage.csv", "wb")
reward_file = open("stage_reward.csv", "wb")
stage_csv = csv.writer(stage_file)
reward_csv = csv.writer(reward_file)
stage_csv.writerow(STAGE_FIELD_NAMES)
reward_csv.writerow(REWARD_FIELD_NAMES)

## LOGIC ##
stage_id = 1
for world, monster_list in WORLD_MONSTERS.items():
    for stage in range(1, STAGES_PER_WORLD+1):
        # exp per stage
        exp = BASE_EXP + ADDITIONAL_EXP_PER_STAGE * stage
        # monster level increases 1 per 3 stages
        level = int((stage-1) / STAGES_PER_MONSTER_LV) + 1
        dstage = (stage-1) % int(STAGES_PER_MONSTER_LV) # between 0 and 2
        boss_id = BOSS_LOCATION.get(world,{}).get(stage)
        # number of waves varies from 5 to 8
        waves = random.randrange(
            MIN_WAVES,
            int(MAX_WAVES + (MAX_WAVES - MIN_WAVES) * dstage / STAGES_PER_MONSTER_LV))
        monster_parts = []
        for wave in range(1, waves+1):
            is_last_wave = (wave == waves)
            is_boss_wave = int(bool(is_last_wave and boss_id))

            monster_picked = []

            monster_count = random.randrange(3,min(3+wave,6))
            if is_boss_wave:
                monster_count = 4
            # add 3 to 5 monsters
            for idx in range(0, monster_count):
                if (idx == 0) and is_boss_wave:
                    monster_picked.append(boss_id)
                else:
                    random_monster = random.choice(monster_list)
                    monster_picked.append(random_monster)
                    monster_parts.append(MONSTER_PARTS[random_monster])

            # sort monsters by count
            monster_count = Counter(monster_picked)
            monster_ids = monster_count.keys()
            monster_ids.sort()

            # add row
            row = [stage_id, world, stage, wave]
            for i in range(0, 4):
                if (i < len(monster_ids)):
                    mid = monster_ids[i]
                    row += [mid, level, '', monster_count[mid]]
                else:
                    row += ['', '', '', '']
            if is_last_wave: # if last wave, check if boss, add exp and reward
                row += [is_boss_wave, stage_id, exp]
            else:
                row += [0, 0, 0]
            stage_csv.writerow(row)

        # setup stage reward
        reward_row = [stage_id]
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
        reward_csv.writerow(reward_row)
        stage_id += 1
