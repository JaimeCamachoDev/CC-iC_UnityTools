using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MTAssets.SkinnedMeshCombiner
{
    public class ExampleAPI : MonoBehaviour
    {

        public Text statusL;
        public Text statusR;
        public Text statusM;
        public Text buttonL;
        public Text buttonR;
        public Text buttonM;
        public SkinnedMeshCombiner charL;
        public SkinnedMeshCombiner charR;
        public SkinnedMeshCombiner charM;

        void Update()
        {
            //Example of API to verify if meshes are merged
            if (charL.isMeshesCombined() == true)
            {
                buttonL.text = "Undo Merge of This Model!";
                statusL.text = "<color=green>Meshes Combined</color>";
            }
            else
            {
                buttonL.text = "Combine This Model!";
                statusL.text = "<color=red>Meshes NOT Combined</color>";
            }

            if (charR.isMeshesCombined() == true)
            {
                buttonR.text = "Undo Merge of This Model!";
                statusR.text = "<color=green>Meshes Combined</color>";
            }
            else
            {
                buttonR.text = "Combine This Model!";
                statusR.text = "<color=red>Meshes NOT Combined</color>";
            }

            if (charM.isMeshesCombined() == true)
            {
                buttonM.text = "Undo Merge of This Model!";
                statusM.text = "<color=green>Meshes Combined</color>";
            }
            else
            {
                buttonM.text = "Combine This Model!";
                statusM.text = "<color=red>Meshes NOT Combined</color>";
            }
        }

        public void ButtonL()
        {
            //Example of API to merge and undo merge
            if (charL.isMeshesCombined() == true)
            {
                charL.UndoCombineMeshes(true, true);
            }
            else
            {
                charL.CombineMeshes();
            }
        }

        public void ButtonR()
        {
            //Example of API to merge and undo merge
            if (charR.isMeshesCombined() == true)
            {
                charR.UndoCombineMeshes(true, true);
            }
            else
            {
                charR.CombineMeshes();
            }
        }

        public void ButtonM()
        {
            //Example of API to merge and undo merge
            if (charM.isMeshesCombined() == true)
            {
                charM.UndoCombineMeshes(true, true);
            }
            else
            {
                charM.CombineMeshes();
            }
        }

        public void OnDone()
        {
            Debug.Log("Meshes merged.");
        }

        public void OnUndo()
        {
            Debug.Log("Meshes unmerged.");
        }
    }
}