namespace SiPVLib.Config.Configs
{
    [System.Serializable]
    public class GameItem
    {
        [ConfigRef(typeof(ConfigInventoryItem))]    
        public string inventoryId;
        public long amount;
    }
}