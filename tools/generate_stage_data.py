import random
import math
import csv
from collections import Counter, OrderedDict
from operator import itemgetter

### EDIT THIS ###
FULL_NORMAL_MONSTER_LIST = [201000, 201001, 201002, 201003, 202000, 202001, 201000, 201001, 201002, 201003, 201000, 201001]
SMALL_NORMAL_MONSTER_LIST = [201000, 201001, 201002, 201003, 201000, 201001, 201002, 201003, 201000, 201001]
STAGE_ID_TO_BOSS_ID = {
    4: 202020
}
# monster_id -> parts_id
MONSTER_ID_TO_PARTS_ID = {
    201000: 234,
    201001: 234,
    201002: 234,
    201003: 234,
    202000: 234,
    202001: 234,
    202020: 234,
}
# non parts materials for each stage
STAGE_ID_TO_NON_PARTS_MATERIAL_ID = {
    4: 101010
}
FIXED_DROP_RATE = 0.15 # doesn't apply for bosses

WORLD_ID_TO_MONSTER_ID = {
    1: [],
    2: [],
    3: [],
}

## OTHER CONSTS ##
#REWARD_BASE = 101000
STAGE_FIELD_NAMES = [
    "id","stage","wave",
    "monster1_id","monster1_level","monster1_visual","monster1_count",
    "monster2_id","monster2_level","monster2_visual","monster2_count",
    "monster3_id","monster3_level","monster3_visual","monster3_count",
    "monster4_id","monster4_level","monster4_visual","monster4_count",
    "is_boss","reward","Exp"
]

REWARD_FIELD_NAMES = [
    "id","",
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
for stage in range(1, 200):
    # exp per stage
    exp = 30 + 5 * stage
    # level increases 1 per 3 stages
    level = int((stage-1) / 3) + 1
    dstage = (stage-1) % 3 # between 0 and 2
    boss_id = STAGE_ID_TO_BOSS_ID.get(stage_id)
    # number of waves varies from 5 to 8
    waves = range(1, random.randrange(5+dstage, math.ceil(6+dstage*1.5)))
    monster_parts = []
    for wave in waves:
        is_last_wave = (wave == waves[-1])
        #print(waves[-1])
        is_boss_wave = int(bool(is_last_wave and boss_id))

        monster_picked = []
        if is_boss_wave:
            # boss level
            monster_picked.append(STAGE_ID_TO_BOSS_ID[stage_id])
        else:
            # regular level
            # there are 2 to 5 regular monsters in each wave
            monster_count = random.randrange(2,min(3+wave,6))
            for _ in range(0, monster_count):
                if (stage < 6): # in the beginning, only show small monster list
                    monster_picked.append(random.choice(SMALL_NORMAL_MONSTER_LIST))
                else:
                    monster_picked.append(random.choice(FULL_NORMAL_MONSTER_LIST))
        if not is_boss_wave:
            # assign parts, boss wave is taken care of separately since it must be included
            monster_parts += map(lambda mid: MONSTER_ID_TO_PARTS_ID[mid], monster_picked)
        monsters = Counter(monster_picked)

        keys = monsters.keys()
        keys.sort()

        # add row
        row = [stage_id, stage, wave]
        for i in range(0, 4):
            if (i < len(keys)):
                row += [keys[i], level, '', monsters[keys[i]]]
            else:
                row += ['', '', '', '']
        if is_last_wave: # if last wave, check if boss, add exp and reward
            row += [is_boss_wave, stage_id, exp]
        else:
            row += [0, 0, 0]
        stage_csv.writerow(row)

    # setup reward
    reward_row = [stage_id, ""]
    parts = Counter(monster_parts)
    # five rows, prioritize stage specific reward and boss parts reward
    if STAGE_ID_TO_NON_PARTS_MATERIAL_ID.get(stage_id):
        reward_id = STAGE_ID_TO_NON_PARTS_MATERIAL_ID.get(stage_id)
        reward_row += [reward_id, 1, 1, 2]
    if boss_id:
        reward_row += [MONSTER_ID_TO_PARTS_ID[boss_id], 1, 1, 2]
    # add rest of them
    parts_sorted_by_count = OrderedDict(sorted(parts.items(), key=itemgetter(1)))
    for parts_id in parts_sorted_by_count:
        if len(reward_row) < len(REWARD_FIELD_NAMES):
            parts_cnt = max(FIXED_DROP_RATE * parts_sorted_by_count[parts_id], 1)
            reward_row += [parts_id, 1, parts_cnt, parts_cnt+1]
    reward_csv.writerow(reward_row)
    stage_id += 1
