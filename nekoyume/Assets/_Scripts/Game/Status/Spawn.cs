using System.Collections;
using UnityEngine;

using DG.Tweening;


namespace Nekoyume.Game.Status
{
    public class Spawn : Base
    {
        public override IEnumerator Execute(Stage stage,  Model.BattleStatus status)
        {
            GameObject go = new GameObject(status.name);
            go.transform.parent = stage.transform;

            Character character = go.AddComponent<Character>();
            character.id = status.id_;
            character.group = status.character_type;
            character._Load(go, status.class_);
            yield return null;
        }
    }
}
