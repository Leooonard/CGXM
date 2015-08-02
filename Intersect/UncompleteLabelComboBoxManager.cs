using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace Intersect
{
    public class UncompleteLabelComboBoxManager
    {
        public const string HIDDEN_TITLE = "*hidden*";

        private bool isInited;
        public bool textChanged
        {
            get;
            set;
        }
        public ObservableCollection<string> chooseableCityPlanStandardInfoList
        {
            get;
            set;
        }
        public UncompleteLabelComboBoxManager()
        {
            isInited = false;
            chooseableCityPlanStandardInfoList = new ObservableCollection<string>();
            ObservableCollection<CityPlanStandard> tempList = CityPlanStandard.GetAllCityPlanStandard();
            foreach (CityPlanStandard cityPlanStandard in tempList)
            {
                chooseableCityPlanStandardInfoList.Add(cityPlanStandard.getCityPlanStandardInfo());
            }
        }
        public bool getIsInited()
        {
            return isInited;
        }
        public void Inited()
        {
            isInited = true;
        }
        public void showHiddenItem(string content)
        {
            string hiddenContent = HIDDEN_TITLE + content;
            for (int i = 0; i < chooseableCityPlanStandardInfoList.Count; i++)
            {
                if (chooseableCityPlanStandardInfoList[i] == hiddenContent)
                {
                    chooseableCityPlanStandardInfoList[i] = chooseableCityPlanStandardInfoList[i].Substring(HIDDEN_TITLE.Length);
                    return;
                }
            }
        }
        public void hideItem(string content)
        {
            for (int i = 0; i < chooseableCityPlanStandardInfoList.Count; i++)
            {
                if (chooseableCityPlanStandardInfoList[i] == content)
                {
                    chooseableCityPlanStandardInfoList[i] = HIDDEN_TITLE + chooseableCityPlanStandardInfoList[i];
                    return;
                }
            }
        }
        public void pruningChooseableCityPlanStandardInfoList(ObservableCollection<string> list)
        {
            foreach (string str in list)
            {
                chooseableCityPlanStandardInfoList.Remove(str);
            }
        }

        public static bool IsUncompleteLabelComboBoxTextRepeat(ObservableCollection<Label> uncompleteLabelList, Label targetLabel, string content)
        {
            bool result = false;
            foreach (Label label in uncompleteLabelList)
            {
                if (label.mapLayerName == targetLabel.mapLayerName)
                    continue;
                if (label.content == content)
                {
                    result = true;
                    break;
                }
            }
            return result;
        }

        public static void ShowHiddenUncompleteLabelComboBoxItem(ObservableCollection<Label> uncompleteLabelList, Label targetLabel, string prevContent)
        {
            foreach (Label label in uncompleteLabelList)
            {
                if (label.mapLayerName == targetLabel.mapLayerName)
                    continue;
                label.uncomleteLabelContentManager.showHiddenItem(prevContent);
            }
        }

        public static void HideUncompleteLabelComboBoxItem(ObservableCollection<Label> uncompleteLabelList, Label targetLabel, string content)
        {
            foreach (Label label in uncompleteLabelList)
            {
                if (label.mapLayerName == targetLabel.mapLayerName)
                    continue;
                label.uncomleteLabelContentManager.hideItem(content);
            }
        }

        public static bool IsContentCityPlanStandard(string content)
        {
            ObservableCollection<CityPlanStandard> tempList = CityPlanStandard.GetAllCityPlanStandard();
            foreach (CityPlanStandard cityPlanStandard in tempList)
            {
                if (cityPlanStandard.getCityPlanStandardInfo() == content)
                    return true;
            }
            return false;
        }
    }
}
