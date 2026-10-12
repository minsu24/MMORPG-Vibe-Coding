using System;
using System.Linq;
using EasternFantasy.Inventory;
using UnityEditor;
using UnityEngine;
namespace EasternFantasy.EditorTools
{
    public sealed class ItemGradePreview : EditorWindow
    {
        private readonly string[] items = {"OldFan","MonkGauntlets","OldHat","ClothTop","ClothBottom","ClothGloves","ClothShoes"};
        private Vector2 scroll;
        [MenuItem("Eastern Fantasy/Inventory/Preview Item Grades")]
        public static void Open() => GetWindow<ItemGradePreview>("아이템 등급");
        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.HelpBox("신화 > 설화 > 영웅 > 희귀 > 일반. 포션은 등급 없음. 아이콘과 프레임은 별도 스프라이트입니다.", MessageType.Info);
            for (int g=4; g>=0; g--)
            {
                EditorGUILayout.LabelField(ItemGradeUtility.GetName((ItemGrade)g), EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                foreach (string name in items)
                {
                    string suffix = g==0 ? "" : "_" + ((ItemGrade)g).ToString();
                    string path = "Assets/_Project/Data/Items/" + (g==0 ? "" : "Grades/") + name + suffix + ".asset";
                    ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
                    EditorGUILayout.BeginVertical(GUILayout.Width(104));
                    Rect area = GUILayoutUtility.GetRect(96,96);
                    if (item != null)
                    {
                        DrawSprite(area,item.GradeFrame);
                        DrawSprite(new Rect(area.x+9,area.y+9,78,78),item.Icon);
                        if (GUILayout.Button(item.DisplayName,GUILayout.Width(100))) Selection.activeObject=item;
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }
        private static void DrawSprite(Rect rect,Sprite sprite)
        {
            if (sprite == null) return;
            Rect uv = sprite.rect;
            uv.x /= sprite.texture.width; uv.width /= sprite.texture.width;
            uv.y /= sprite.texture.height; uv.height /= sprite.texture.height;
            float ratio = sprite.rect.width/sprite.rect.height;
            if (ratio>1) { float h=rect.width/ratio; rect.y+=(rect.height-h)/2; rect.height=h; }
            else { float w=rect.height*ratio; rect.x+=(rect.width-w)/2; rect.width=w; }
            GUI.DrawTextureWithTexCoords(rect,sprite.texture,uv,true);
        }
        public static string Validate()
        {
            string[] names={"OldFan","OldHat","ClothTop","ClothBottom","ClothGloves","ClothShoes"};
            foreach(string name in names)
            {
                ItemDefinition original=AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/"+name+".asset");
                if(original.Grade!=ItemGrade.Common) throw new Exception("Original grade changed: "+name);
                for(int g=0;g<5;g++)
                {
                    ItemDefinition variant=original.GetGradeVariant((ItemGrade)g);
                    if(variant==null || variant.Icon==null || variant.GradeFrame==null) throw new Exception("Missing grade visual: "+name+g);
                    if(variant.AttackBonus!=original.AttackBonus || variant.DefenseBonus!=original.DefenseBonus) throw new Exception("Stats changed");
                    if(g>0 && variant.ItemId==original.ItemId) throw new Exception("Duplicate item ID");
                    var copy=UnityEngine.Object.Instantiate(original);
                    var so=new SerializedObject(copy); so.FindProperty("grade").intValue=g; so.ApplyModifiedPropertiesWithoutUndo();
                    bool resolves=copy.Icon==variant.Icon && copy.GradeFrame==variant.GradeFrame;
                    UnityEngine.Object.DestroyImmediate(copy);
                    if(!resolves) throw new Exception("Grade did not resolve icon and frame together");
                }
            }
            foreach(string name in new[]{"HealthPotion","ManaPotion"})
            {
                var potion=AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/"+name+".asset");
                if(potion.HasGrade || potion.Grade!=ItemGrade.None || potion.GradeFrame!=null || potion.GetGradeVariant(ItemGrade.Rare)!=null)
                    throw new Exception("Potion received a grade");
            }
            var root=new GameObject("GradeValidation"); root.SetActive(false);
            try
            {
                var inv=root.AddComponent<PlayerInventory>();
                var fan=AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/OldFan.asset");
                if(!inv.AddItem(fan) || !inv.TryEquip(fan) || !inv.TryChangeGrade(fan,ItemGrade.Mythic)) throw new Exception("Grade replacement failed");
                var mythic=fan.GetGradeVariant(ItemGrade.Mythic);
                if(inv.GetQuantity(fan)!=0 || inv.GetQuantity(mythic)!=1 || inv.GetEquipped(EquipmentSlot.Weapon)!=mythic || fan.Grade!=ItemGrade.Common)
                    throw new Exception("Replacement changed shared item or failed to update equipment");
                if(inv.TryChangeGrade(mythic,ItemGrade.Mythic)) throw new Exception("Same-grade operation succeeded");
                if(!inv.TryChangeGrade(mythic,ItemGrade.Common) || inv.GetEquipped(EquipmentSlot.Weapon)!=fan) throw new Exception("Downgrade failed");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            return "PASS: 30 equipment visuals, paired grade changes, potion exclusions, unique IDs, preserved stats, equipped upgrade/downgrade, shared definition isolation.";
        }
    }
}
