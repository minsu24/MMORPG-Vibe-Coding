namespace EasternFantasy.UI
{
    public static class MapLocationNames
    {
        public static string GetDisplayName(string sceneName)
        {
            switch (sceneName)
            {
                case "MainScreen":
                case "ChooseCharactor": return string.Empty;
                case "MovementPrototype": return "마을 외곽";
                case "PrologueVilage": return "서막 마을";
                case "Cemetery": return "공동묘지";
                case "SpiderCave": return "거미동굴";
                case "Marshland": return "늪지대";
                case "BrokenSeal": return "부서진 봉인";
                case "GapInSpace": return "공간의 틈";
                case "GiantFarm_01": return "거인의 농장 · 초입";
                case "GiantFarm_02": return "거인의 농장 · 안쪽";
                case "GiantFarm_03_Boss": return "거인의 농장 · 거인의 거처";
                default: return sceneName;
            }
        }
    }
}
