using System.Collections.Generic;
using System.IO;
using System.Linq;
using DG.Tweening.Plugins.Core.PathCore;
using Nekoyume.Model;
using UnityEngine;

namespace Nekoyume.Action
{
    public class Simulator
    {
        public readonly List<CharacterBase> characters;
        private readonly int _seed;
        private readonly int _stage;
        private int time = 0;
        private string result;
        private bool isWin = true;
        private bool isLose = true;

        public Simulator(int seed, Model.Avatar avatar)
        {
            //TODO generate random using seed
            _seed = seed;
            _stage = avatar.WorldStage;
            characters = new List<CharacterBase>();
            var player = new Player(avatar);
            var monster = new Monster{target = player};
            player.target = monster;
            Add(player);
            Add(monster);
        }

        public Player Simulate()
        {
            foreach (var character in characters)
            {
                //TODO spawn
                Debug.Log(character);
            }

            while (true)
            {
                foreach (var character in characters)
                {
                    character.Tick();
                }
                time++;
                if (time >= 100)
                {
                    result = "finish";
                    break;
                }

                foreach (var character in characters)
                {
                    if (character is Monster)
                    {
                        if (!character.isDead)
                        {
                            isWin = false;
                        }
                    }
                    if (character is Player)
                    {
                        if (!character.isDead)
                        {
                            var player = (Player) character;
                            player.stage++;
                            isLose = false;
                        }
                    }
                }

                if (isWin)
                {
                    result = "win";
                    Debug.Log("win");
                    break;
                }

                if (isLose)
                {
                    result = "lose";
                    Debug.Log("lose");
                    break;
                }
            }
            return (Player) characters.First(c => c is Player);
        }

        private void Add(CharacterBase character)
        {
            character.InitAI();
            characters.Add(character);
        }
    }
}
