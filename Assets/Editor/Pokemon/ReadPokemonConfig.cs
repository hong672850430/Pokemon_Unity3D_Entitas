﻿using DataFileManager;
using Newtonsoft.Json;
using OfficeOpenXml;
using PokemonBattelePokemon;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public class ReadPokemonConfig :Editor
{

    
    public static Dictionary<int, Race> allRaceDic = new Dictionary<int, Race>();
    private static Dictionary<NatureType, Nature> allNatureDic = new Dictionary<NatureType, Nature>();
    private static Dictionary<int, Ability> allAbilityDic = new Dictionary<int, Ability>();
    private static Dictionary<int, int> allCatchRateDic = new Dictionary<int, int>();
 
    private static Dictionary<int, Item> allItemDic = new Dictionary<int, Item>();
    /// <summary>
    ///属性克制表 key为[攻击方,防守方]
    /// </summary>
    private static float[,] TypeInf = new float[19, 19];
    public static string SkillJsonpath = "Assets/Resources/ReadTxt/PokemonSkills.json";
    public static string NatureJsonpath = "Assets/Resources/ReadTxt/PokemonNatures.json";
    public static string RaceJsonpath = "Assets/Resources/ReadTxt/PokemonRaces.json";
    public static string AbilityJsonpath = "Assets/Resources/ReadTxt/PokemonAbilitys.json";
    private static string TypeInfoJsonpath = "Assets/Resources/ReadTxt/PokemonTypeInfos.json";
    private static string CatchRateJsonpath = "Assets/Resources/ReadTxt/CatchRates.json";
    private static string BagItemJsonpath= "Assets/Resources/ReadTxt/BagItems.json";

    private static string skillSavePath = "Assets/Resources/SkillAsset/";
    private static string SkillConfigPath = "Assets/Skill/SkillConfig.json";




  
    /// <summary>
    /// 委托事件:从Excel加载数据
    /// </summary>
    [MenuItem("Pokemon/读取精灵配置Excel")]
    public static void ReadPokemonOtherConfigFromExcel()
    {
        allRaceDic.Clear();

        //FileInfo fi = new FileInfo(Application.dataPath + @"/Resources/race.xlsx");
        FileStream stream = new FileStream(Application.dataPath + @"/Resources/ReadTxt/PokemonConfigurationTable.xlsx", FileMode.Open);


        using (ExcelPackage package = new ExcelPackage(stream))
        {
            int size = package.Workbook.Worksheets.Count;
            ExcelWorksheet worksheet;
            try
            {
                for (int i = 1; i <= size; i++)
                {
                    worksheet = package.Workbook.Worksheets[i];

                    switch (worksheet.Name)
                    {
                        case "种族":
                            ReadRaceFromExcel(worksheet);
                            break;
                        case "性格":
                            ReadNatureFromExcel(worksheet);
                            break;
                        case "特性":
                            ReadAbilityFromExcel(worksheet);
                            break;
                        case "属性克制":
                            ReadTypeInfluence(worksheet);
                            break;
                        case "捕获率":
                            ReadCatchRateFromExcel(worksheet);
                            break;
                        case "道具":
                            ReadBagItemsFromExcel(worksheet);
                            break;
                    }

                }
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
                return;
            }


        }


        //删除进度条
        EditorUtility.ClearProgressBar();
    }

    /// <summary>
    ///读取精灵道具数据
    /// </summary>
    private static void ReadBagItemsFromExcel(ExcelWorksheet worksheet)
    {
        var cells = worksheet.Cells;

        int row = worksheet.Dimension.Start.Row; //The item description

        int id = 1;
        string name, description,type;
        ItemType itemType=0;
        for (int n = worksheet.Dimension.End.Row; row <= n; row++)
        {
            EditorUtility.DisplayCancelableProgressBar("读取精灵道具数据", (row - 1) + "/" + n, (float)row / n);
            try
            {
                name= cells[row, 1].Value.ToString();
                type = cells[row, 2].Value.ToString();
                description = cells[row, 3].Value.ToString();
                
                switch(type)
                {
                    case "精灵球":
                        itemType = ItemType.PokemonBall;
                        break;
                    case "树果":
                        itemType = ItemType.TreeFruit;
                        break;
                    case "携带道具":
                        itemType = ItemType.CarryingProps;
                        break;
                    case "消耗品":
                        itemType = ItemType.Consumables;
                        break;

                }
                Item item = new Item(id, name, itemType, description);
                allItemDic[id] = item;
                
            }
            catch (Exception e)
            {
                Debug.Log("第" + row + "行数据有误" + e.Message);
                continue;
            }
            
            id++;
        }
        string json = JsonConvert.SerializeObject(allItemDic);
        File.WriteAllText(BagItemJsonpath, json, Encoding.UTF8);
        Debug.Log("道具数据已加载");
    }

        /// <summary>
        /// 判断精灵属性
        /// </summary>
        /// <param name="s"></param>
        /// <param name="maintype"></param>
        /// <param name="secondType"></param>
    private static void GetPokemonTypeFromStr(string s, out PokemonType mainType, out PokemonType secondType)
    {
        //string ones = s.Substring(0, 1);
        List<string> allEnumStr = new List<string>();

        foreach (string temp in Enum.GetNames(typeof(PokemonType))) allEnumStr.Add(temp);
        bool isMainTypeGet = false, isSecondValGet = false;
        mainType = secondType = PokemonType.NULL;
        var enumerator = allEnumStr.GetEnumerator();
        try
        {
            while (enumerator.MoveNext())
            {

                var temp = enumerator.Current;
                if (s.IndexOf(temp) > 0)
                {
                    secondType = (PokemonType)Enum.Parse(typeof(PokemonType), temp);
                    isMainTypeGet = true;
                }
                else if (s.IndexOf(temp) == 0)
                {
                    mainType = (PokemonType)Enum.Parse(typeof(PokemonType), temp);
                    isSecondValGet = true;
                }
                if (isMainTypeGet && isSecondValGet) return;
            }
        }
        finally
        {
            enumerator.Dispose();
        }
        
        

    }

    /// <summary>
    ///读取精灵种族数据
    /// </summary>
    private static void ReadRaceFromExcel(ExcelWorksheet worksheet)
    {
        var cells = worksheet.Cells;

        int row = worksheet.Dimension.Start.Row; //The item description
        int id;
        string name;
        int health, ppower, pdefence, epower, edefence, speed;
        PokemonType mainType = PokemonType.NULL, secondType = PokemonType.NULL;
        AbilityType firstAbilityType, secondAbilityType, hideAbilityType;
        firstAbilityType = secondAbilityType = hideAbilityType = AbilityType.NULL;
        for (int n = worksheet.Dimension.End.Row; row <= n; row++)
        {
            EditorUtility.DisplayCancelableProgressBar("读取精灵种族数据", (row - 1) + "/" + n, (float)row / n);
            try
            {
                id = int.Parse(cells[row, 1].Value.ToString().Substring(1));
                name = (string)cells[row, 2].Value;
                string TypeStr = (string)cells[row, 3].Value;

                GetPokemonTypeFromStr(TypeStr, out mainType, out secondType);

                firstAbilityType = (AbilityType)Enum.Parse(typeof(AbilityType), cells[row, 4].Value.ToString());

                if (null != cells[row, 5].Value )
                {
                    secondAbilityType = (AbilityType)Enum.Parse(typeof(AbilityType), cells[row, 5].Value.ToString());
                }
                hideAbilityType = (AbilityType)Enum.Parse(typeof(AbilityType), cells[row, 6].Value.ToString());
                health = int.Parse(cells[row, 7].Value.ToString());
                ppower = int.Parse(cells[row, 8].Value.ToString());
                pdefence = int.Parse(cells[row, 9].Value.ToString());
                epower = int.Parse(cells[row, 10].Value.ToString());
                edefence = int.Parse(cells[row, 11].Value.ToString());
                speed = int.Parse(cells[row, 12].Value.ToString());

                Race race;
                race = new Race(id, name, health, ppower, pdefence, epower, edefence, speed,
                    mainType, secondType, firstAbilityType, secondAbilityType, hideAbilityType);

                //判断一种精灵是否有不同的种族值
                if (allRaceDic.ContainsKey(id))
                {
                    int num = allRaceDic.Count + 1000;
                    allRaceDic.Add(allRaceDic.Count + 1, race);
                    allRaceDic[id].otherRace.Add(num);
                }
                else allRaceDic.Add(id, race);
            }
            catch (Exception e)
            {
                Debug.Log("第" + row + "行数据有误" + e.Message);
                continue;
            }
        }
        //数据太多改成json
        //PokemonRaceScriptableObject serializationDictionary = ScriptableObject.CreateInstance<PokemonRaceScriptableObject>();
        //serializationDictionary.Target = allRaceDic;
        //AssetDatabase.CreateAsset(serializationDictionary, "Assets/Resources/PokemonRaces.asset");

        string json = JsonConvert.SerializeObject(allRaceDic);
        File.WriteAllText(RaceJsonpath, json, Encoding.UTF8);
        Debug.Log("精灵种族数据已加载");
    }

    /// <summary>
    ///读取精灵捕获率数据
    /// </summary>
    private static void ReadCatchRateFromExcel(ExcelWorksheet worksheet)
    {
        var cells = worksheet.Cells;
        int row = worksheet.Dimension.Start.Row;
        int id, rate;
        for (int n = worksheet.Dimension.End.Row; row <= n; row++)
        {
            EditorUtility.DisplayCancelableProgressBar("读取精灵捕获率数据", (row - 1) + "/" + n, (float)row / n);
            try
            {
                id = int.Parse(cells[row, 1].Value.ToString());
                rate = int.Parse(cells[row, 2].Value.ToString());
                allCatchRateDic.Add(id, rate);
            }
            catch (Exception e)
            {
                Debug.Log("第" + row + "行数据有误" + e.Message);
                continue;
            }
        }
        string json = JsonConvert.SerializeObject(allCatchRateDic);
        File.WriteAllText(CatchRateJsonpath, json, Encoding.UTF8);
        Debug.Log("精灵捕获率数据已加载");
    }

    /// <summary>
    /// 读取精灵性格数据
    /// </summary>
    private static void ReadNatureFromExcel(ExcelWorksheet worksheet)
    {
        allNatureDic.Clear();
        var cells = worksheet.Cells;
        int row = worksheet.Dimension.Start.Row;
        NatureType natureType;
        Base6 upBase;
        Base6 downBase;
        string natureName;
        for (int n = worksheet.Dimension.End.Row; row <= n; row++)
        {
            EditorUtility.DisplayCancelableProgressBar("读取精灵性格数据", (row - 1) + "/" + (n - 1), (float)row / (n - 1));

            try
            {
                natureName = cells[row, 1].Value.ToString();
                natureType = (NatureType)Enum.Parse(typeof(NatureType), cells[row, 1].Value.ToString());
                if (cells[row, 4].Value.ToString() == "—" || cells[row, 4].Value.ToString() == "—")
                {
                    downBase = upBase = Base6.NULL;
                }
                else
                {
                    upBase = (Base6)Enum.Parse(typeof(Base6), cells[row, 4].Value.ToString());
                    downBase = (Base6)Enum.Parse(typeof(Base6), cells[row, 5].Value.ToString());
                }

            }
            catch (Exception e)
            {
                Debug.Log("第" + row + "行数据有误" + e.Message);
                continue;
            }
            Nature nature;
            nature = new Nature() { natureType = natureType, upAtt = upBase, downAtt = downBase };
            allNatureDic.Add(natureType, nature);
        }
        //PokemonNatureScriptableObject serializationDictionary = CreateInstance<PokemonNatureScriptableObject>();
        //serializationDictionary.Target = allNatureDic;
        //AssetDatabase.CreateAsset(serializationDictionary, "Assets/Resources/PokemonNatures.asset");
        string json = JsonConvert.SerializeObject(allNatureDic);
        File.WriteAllText(NatureJsonpath, json, Encoding.UTF8);
        Debug.Log("精灵性格数据已加载");
    }

    /// <summary>
    /// 读取精灵特性数据
    /// </summary>
    /// <param name="worksheet"></param>
    private static void ReadAbilityFromExcel(ExcelWorksheet worksheet)
    {
        allAbilityDic.Clear();
        var cells = worksheet.Cells;
        int row = worksheet.Dimension.Start.Row;
        AbilityType abilityType;
        string description;
        int id;
        for (int n = worksheet.Dimension.End.Row; row <= n; row++)
        {
            EditorUtility.DisplayCancelableProgressBar("读取精灵特性数据", (row - 1) + "/" + (n - 1), (float)row / (n - 1));

            try
            {
                id = int.Parse(cells[row, 1].Value.ToString());
                abilityType = (AbilityType)Enum.Parse(typeof(AbilityType), cells[row, 2].Value.ToString());
                description = cells[row, 5].Value.ToString();

            }
            catch (Exception e)
            {
                Debug.Log("第" + row + "行数据有误" + e.Message);
                continue;
            }
            Ability ability;
            ability = new Ability() { abilityType = abilityType, description = description };
            allAbilityDic.Add(id, ability);
        }
        //PokemonAbilityScriptableObject serializationDictionary = CreateInstance<PokemonAbilityScriptableObject>();
        //serializationDictionary.Target = allAbilityDic;
        //AssetDatabase.CreateAsset(serializationDictionary, "Assets/Resources/PokemonAbilitys.asset");
        string json = JsonConvert.SerializeObject(allAbilityDic);
        File.WriteAllText(AbilityJsonpath, json, Encoding.UTF8);
        Debug.Log("精灵特性数据已加载");
    }

    /// <summary>
    /// 读取属性影响
    /// </summary>
    public static void ReadTypeInfluence(ExcelWorksheet worksheet)
    {
        var cells = worksheet.Cells;
        int row = 3;
        int col = 3;
        int m = worksheet.Dimension.End.Column;
        for (int n = worksheet.Dimension.End.Row; row <= n; row++)
        {
            for (col = 3; col <= m; col++)
            {
                string s = cells[row, col].Value.ToString();
                switch (s)
                {
                    case "1×":
                        TypeInf[row - 2, col - 2] = 1;
                        break;
                    case "2×":
                        TypeInf[row - 2, col - 2] = 2;
                        break;
                    case "1⁄2×":
                        TypeInf[row - 2, col - 2] = 0.5f;
                        break;
                    default:
                        TypeInf[row - 2, col - 2] = 0;
                        break;
                }
            }

        }
        string json = JsonConvert.SerializeObject(TypeInf);
        File.WriteAllText(TypeInfoJsonpath, json, Encoding.UTF8);
        Debug.Log("属性克制数据已加载");
    }
}
