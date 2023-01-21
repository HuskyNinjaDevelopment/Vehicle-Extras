using CitizenFX.Core;
using CitizenFX.Core.Native;
using FivePD.API;
using Newtonsoft.Json.Linq;
using System;

namespace VehicleExtras
{
    internal class VehicleExtras : Plugin
    {
        //Config Data
        private string _vehicleField = "vehicle";
        private string _liveryField = "livery";
        private string _extrasField = "extras";
        private string _callsignField = "setPlateCallsign";

        private int _maxExtras = 13;

        //Game Data
        private Vehicle _playerVehicle = null;

        internal VehicleExtras()
        {
            AddEventHandlers();
        }
        private void AddEventHandlers()
        {
            EventHandlers["FivePD::Client::SpawnVehicle"] += new Action<int, int>(SetUpVehicle);
        }
        private void SetUpVehicle(int serverID, int networkID)
        {
            //Get Spawned Vehicle
            _playerVehicle = Game.PlayerPed.LastVehicle;

            //Parse Vehicles JSON into a JToken to iterate through later
            string config = ReadVehiclesJson();
            var jsonConfig = JObject.Parse(config);
            JToken policeVehiclesJson = jsonConfig["police"];

            Extras(policeVehiclesJson);
            LicensePlate(policeVehiclesJson);
            Livery(policeVehiclesJson);
        }
        //Vehicle Extras
        private void Extras(JToken tokenizedJson)
        {
            if(_playerVehicle == null) { return; }

            JToken extras = null;
            foreach(var item in tokenizedJson)
            {
                if (item[_vehicleField].ToString() == _playerVehicle.DisplayName) 
                {
                    extras = item[_extrasField];
                }
            }

            if (extras != null)
            {
                //There are 12 extras to enable/disable
                switch(extras.ToString())
                {
                    case "all":
                        for (int i = 1; i < _maxExtras; i++)
                        {
                            ToggleExtra(_playerVehicle, i, true);
                        }
                        break;

                    case "none":
                        for (int i = 1; i < _maxExtras; i++)
                        {
                            ToggleExtra(_playerVehicle, i, false);
                        }
                        break;

                    default:
                        //Vehicle has a specific set of extras on it
                        for(int i = 1; i < _maxExtras; i++)
                        {
                            ToggleExtra(_playerVehicle, i, false);
                        }

                        //Loop through all the listed extras
                        foreach(int id in extras)
                        {
                            //Check if the extra exists
                            if(_playerVehicle.ExtraExists(id))
                            {
                                ToggleExtra(_playerVehicle, id, true);
                            }
                        }

                        break;
                }
            }
            else
            {
                Debug.WriteLine($"No 'extras' Field found in 'vehicles.json' for {_playerVehicle.DisplayName}");
            }
        }
        private void ToggleExtra(Vehicle veh, int extraID, bool status)
        {
            //Determine if the extra exists before enable/disable
            if (veh.ExtraExists(extraID))
            {

                veh.ToggleExtra(extraID, status);
            }
        }

        //License Plate
        private void LicensePlate(JToken tokenizedJson)
        {
            if (_playerVehicle == null) { return; }

            JToken tokenExists = null;
            foreach (var item in tokenizedJson)
            {
                if (item[_vehicleField].ToString() == _playerVehicle.DisplayName)
                {
                    tokenExists = item[_callsignField];
                }
            }

            //Check if the token even exists
            if (tokenExists != null) 
            {
                if (tokenExists.Value<bool>())
                {
                    API.SetVehicleNumberPlateText(_playerVehicle.Handle, Utilities.GetPlayerData().Callsign);
                }
                else
                {
                    Debug.WriteLine($"'setPlateCallsign' field set to 'false' for {_playerVehicle.DisplayName}");
                }
            }
            else
            {
                Debug.WriteLine($"No 'setPlateCallsign' Field found in 'vehicles.json' for {_playerVehicle.DisplayName}");
            }
        }

        //Livery
        private void Livery(JToken tokenizedJson)
        {
            if (_playerVehicle == null) { return; }

            JToken livery = null;
            foreach (var item in tokenizedJson)
            {
                if (item[_vehicleField].ToString() == _playerVehicle.DisplayName)
                {
                    livery = item[_liveryField];
                }
            }

            if (livery != null)
            {
                API.SetVehicleLivery(_playerVehicle.Handle, (int)livery);
            }
            else
            {
                Debug.WriteLine($"No 'livery' Field found in 'vehicles.json' for {_playerVehicle.DisplayName}");
            }
        }

        //Utils
        private string ReadVehiclesJson()
        {
            return API.LoadResourceFile(API.GetCurrentResourceName(), "config/vehicles.json");
        }
    }
}
