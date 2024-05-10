using Cysharp.Threading.Tasks;
using Nekoyume.Model.AdventureBoss;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class WorldMapAdventureBoss : MonoBehaviour
    {
        [SerializeField] private GameObject Open;
        [SerializeField] private GameObject WantedOpen;
        [SerializeField] private GameObject WantedClose;
        [SerializeField] private GameObject Close;

        [SerializeField] private TextMeshProUGUI[] RemainingBlockIndexs;
        [SerializeField] private TextMeshProUGUI UsedNCG;
        [SerializeField] private TextMeshProUGUI Floor;


        private readonly List<System.IDisposable> _disposables = new();

        private void Awake()
        {
            Game.Game.instance.AdventureBossData.CurrentSeasonInfo.Subscribe(OnSeasonInfoChanged);
        }

        private void OnEnable()
        {
            Game.Game.instance.Agent.BlockIndexSubject
                .Subscribe(UpdateViewAsync)
                .AddTo(_disposables);
        }

        private void UpdateViewAsync(long blockIndex)
        {
            var seasonInfo = Game.Game.instance.AdventureBossData.CurrentSeasonInfo.Value;
            if (seasonInfo == null)
            {
                foreach (var text in RemainingBlockIndexs)
                {
                    text.text = "";
                }
                return;
            }
            var remainingIndex =  seasonInfo.EndBlockIndex - blockIndex;
            var timeText = $"{remainingIndex:#,0}({remainingIndex.BlockRangeToTimeSpanString()})";
            foreach (var text in RemainingBlockIndexs)
            {
                text.text = timeText;
            }
        }

        private void OnDisable()
        {
            _disposables.DisposeAllAndClear();
        }

        private void OnSeasonInfoChanged(SeasonInfo info)
        {
            if (info == null)
            {
                Close.SetActive(true);
                Open.SetActive(false);
                return ;
            }

            if (info.EndBlockIndex < Game.Game.instance.Agent.BlockIndex)
            {
                Open.SetActive(false);
                Close.SetActive(true);
                return;
            }
            Open.SetActive(true);
            Close.SetActive(false);

            if(info.ParticipantList != null && info.ParticipantList.Count() > 0)
            {
                WantedOpen.SetActive(true);
                WantedClose.SetActive(false);
                UsedNCG.text = info.UsedNcg.ToCurrencyNotation();
                //Floor.text = 
            }
            else
            {
                WantedOpen.SetActive(false);
                WantedClose.SetActive(true);
            }

            return;
        }
    }
}
