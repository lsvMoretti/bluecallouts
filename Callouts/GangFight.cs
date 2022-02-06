using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using FivePD.API;
using FivePD.API.Utils;

namespace BlueLineFivePd
{
    [CalloutProperties("UKGangFight", "Unsociable", "V1.0.0")]
    public class GangFight : Callout
    {
        //TODO: Add more positions
        // Position 1 - Spawn Location / Position 2 - Spawn Location / Position 3 - Arrived Location
        private static readonly List<List<Vector3>> _calloutPositions = new List<List<Vector3>>() {
            new List<Vector3>() { new Vector3(76, -1847, 25), new Vector3(116, -1933, 21), new Vector3(62, -1904, 22)},// Purple home
            new List<Vector3>() { new Vector3(-214, -1604, 35), new Vector3(-149, -1648, 33), new Vector3(-136, -1544, 35)}, // Green home
            new List<Vector3>() { new Vector3(1182, -3035, 6), new Vector3(1082, -3046, 6), new Vector3(1050, -2960, 6)}, // Docks
            new List<Vector3>() { new Vector3(94, -1220, 30), new Vector3 (151, -1199, 30), new Vector3(129, -1246, 30)} // Homeless camp
        };

        #region Ped Types

        private readonly int[] _greenGangHashes =
        {
            -398748745,
            866411749,
            -613248456,
            -2077218039,
            1309468115,
            -449965460,
        };

        private readonly int[] _purpleGangHashes =
        {
            -198252413,
            588969535,
            361513884,
            -1492432238,
            599294057,
        };

        #endregion
        
        private List<Ped> _ballaGangPeds = new List<Ped>();
        private List<Ped> _groveGangPeds = new List<Ped>();

        private List<Vector3> _calloutLocation;

        private string _gangOneString = "GREENGANG";
        private string _gangTwoString = "PURPLEGANG";
        
        public GangFight()
        {
            var rnd = new Random();
            var posIndex = rnd.Next(1 , _calloutPositions.Count) - 1;
            Debug.WriteLine($"Total Positions: {_calloutPositions.Count} - Random Index: {posIndex}");
            var rndPos = _calloutPositions[posIndex];

            _calloutLocation = rndPos;
            
            InitInfo(rndPos[2]);
            
            ShortName = "Gang Fight";
            CalloutDescription = "Reports of two gangs fighting!";
            ResponseCode = 0;
            StartDistance = 75f;
            FixedLocation = false;
        }

        public override async Task OnAccept()
        {
            InitBlip(50f, BlipColor.Blue);

            var gangOneCount = RandomUtils.GetRandomNumber(1, 5);
            var gangTwoCount = RandomUtils.GetRandomNumber(1, 5);

            for (int i = 0; i < gangOneCount; i++)
            {
                var gangPed = await SpawnPed((PedHash) GetRandomGangHash(true), _calloutLocation[0].Around(2f));
                _groveGangPeds.Add(gangPed);
            }

            for (int i = 0; i < gangTwoCount; i++)
            {
                var gangPed = await SpawnPed((PedHash) GetRandomGangHash(false), _calloutLocation[1].Around(2f));
                _ballaGangPeds.Add(gangPed);
            }
        }

        public override async void OnStart(Ped player)
        {
            base.OnStart(player);

            var greenHashKey = (uint)API.GetHashKey(_gangOneString);
            var purpleHashKey = (uint)API.GetHashKey(_gangTwoString);
            var playerHashKey = (uint) API.GetHashKey("PLAYER");
            
            API.AddRelationshipGroup(_gangOneString, ref greenHashKey);
            API.AddRelationshipGroup(_gangTwoString, ref purpleHashKey);
            
            API.SetPedRelationshipGroupHash(player.Handle, playerHashKey);
            
            API.SetRelationshipBetweenGroups(5, greenHashKey, purpleHashKey);
            API.SetRelationshipBetweenGroups(5, purpleHashKey, greenHashKey);

            foreach (var groveGangPed in _groveGangPeds)
            {
                while (!API.DoesEntityExist(groveGangPed.Handle))
                {
                    await BaseScript.Delay(100);
                }
                
                await groveGangPed.TryRequestNetworkEntityControl(false);
                
                API.SetPedRelationshipGroupHash(groveGangPed.Handle, greenHashKey);
            }
            
            foreach (var ballaGangPed in _ballaGangPeds)
            {
                while (!API.DoesEntityExist(ballaGangPed.Handle))
                {
                    await BaseScript.Delay(100);
                }
                
                await ballaGangPed.TryRequestNetworkEntityControl(false);
                
                API.SetPedRelationshipGroupHash(ballaGangPed.Handle, purpleHashKey);
            }
            
            var knifeItem = new Item
            {
                Name = "4 Inch Knife",
                IsIllegal = true
            };

            var knuckleDusterItem = new Item()
            {
                Name = "Knuckle Dusters",
                IsIllegal = true
            };
            
            foreach (var groveGangPed in _groveGangPeds)
            {
                var chanceOfKnife = RandomUtils.GetRandomNumber(1, 11); 
                var pedData = await groveGangPed.GetData();
                if (chanceOfKnife < 6)
                {
                    // Give a knife
                    groveGangPed.Weapons.Give(WeaponHash.Knife, 1, true, true);
                    pedData.Items.Add(knifeItem);
                }
                else
                {
                    groveGangPed.Weapons.Give(WeaponHash.KnuckleDuster, 1, true, true);
                    pedData.Items.Add(knuckleDusterItem);
                }
                
                Utilities.SetPedData(groveGangPed.NetworkId, pedData);
                API.TaskCombatHatedTargetsAroundPed(groveGangPed.Handle, 1000, 0);
            }
            
            foreach (var ballaGangPed in _ballaGangPeds)
            {
                var chanceOfKnife = RandomUtils.GetRandomNumber(1, 11);
                var pedData = await ballaGangPed.GetData();
                if (chanceOfKnife < 6)
                {
                    // Give a knife
                    ballaGangPed.Weapons.Give(WeaponHash.Knife, 1, true, true);

                    pedData.Items.Add(knifeItem);
                }
                else
                {
                    ballaGangPed.Weapons.Give(WeaponHash.KnuckleDuster, 1, true, true);
                    pedData.Items.Add(knuckleDusterItem);
                }
                
                Utilities.SetPedData(ballaGangPed.NetworkId, pedData);
                API.TaskCombatHatedTargetsAroundPed(ballaGangPed.Handle, 1000, 0);
            }
        }

        public override void OnCancelBefore()
        {
            foreach (var groveGangPed in _groveGangPeds)
            {
                if (!API.DoesEntityExist(groveGangPed.Handle)) continue;
                groveGangPed.MarkAsNoLongerNeeded();
            }

            foreach (var ballaGangPed in _ballaGangPeds)
            {
                if (!API.DoesEntityExist(ballaGangPed.Handle)) continue;
                ballaGangPed.MarkAsNoLongerNeeded();
                
            }
        }

        private int GetRandomGangHash(bool isGreenGang)
        {
            var rnd = new Random();
            
            if (isGreenGang)
            {
                var greenIndex = rnd.Next(0, _greenGangHashes.Length);
                return _greenGangHashes[greenIndex];
            }
            
            var purpleIndex = rnd.Next(0, _purpleGangHashes.Length);
            return _purpleGangHashes[purpleIndex];
            
        }
    }
}