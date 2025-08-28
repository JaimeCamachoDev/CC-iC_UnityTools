#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace MTAssets.SkinnedMeshCombiner
{
    /*
     *  This class is responsible for the functioning of the "Skinned Mesh Bones Manager" component, and all its functions.
     */
    /*
     * The Skinned Mesh Combiner was developed by Marcos Tomaz in 2019.
     * Need help? Contact me (mtassets@windsoft.xyz)
     */

    [AddComponentMenu("MT Assets/Skinned Mesh Combiner/Skinned Mesh Bones Manager")] //Add this component in a category of addComponent menu
    public class SkinnedMeshBonesManager : MonoBehaviour
    {
        //Classes of this script
        public class ValidationStatus
        {
            //Stores the validation status informations
            public bool isValid = false;
            public string information = "";

            //Core methods
            public ValidationStatus(bool isValidTarget, string information)
            {
                this.isValid = isValidTarget;
                this.information = information;
            }
        }

        //Enums of this script
        public enum BonesLinkingMethod
        {
            IdenticalHierarchiesOnly,
            IdenticalBonesOnly
        }

        //Public variables of script
        ///<summary>[WARNING] Do not change the value of this variable. This is a variable used for internal tool operations.</summary> 
        [HideInInspector]
        public SkinnedMeshRenderer anotherBonesHierarchyCurrentInUse = null;

#if UNITY_EDITOR
        //Public variables of Interface
        private bool gizmosOfThisComponentIsDisabled = false;

        //Classes of this script, only disponible in Editor
        public class VerticeData
        {
            //This class store all data about a vertice influenced by a bone
            public BoneInfo influencerBone;
            public float weightOfInfluencer;
            public int indexOfThisVerticeInMesh;

            public VerticeData(BoneInfo boneInfo, float weightOfInfluencer, int indexOfThisVerticeInMesh)
            {
                this.influencerBone = boneInfo;
                this.weightOfInfluencer = weightOfInfluencer;
                this.indexOfThisVerticeInMesh = indexOfThisVerticeInMesh;
            }
        }
        public class BoneInfo
        {
            //This class store all data about a bone
            public GameObject gameObject;
            public Transform transform;
            public string name;
            public string transformPath;
            public int hierarchyIndex;
            public List<VerticeData> verticesOfThis = new List<VerticeData>();
        }

        //Public variables of editor 
        ///<summary>[WARNING] This variable is only available in the Editor and will not be included in the compilation of your project, in the final Build.</summary> 
        [HideInInspector]
        private List<BoneInfo> bonesCacheOfThisRenderer = new List<BoneInfo>();
        ///<summary>[WARNING] This variable is only available in the Editor and will not be included in the compilation of your project, in the final Build.</summary> 
        [HideInInspector]
        private BoneInfo boneInfoToShowVertices = null;
        ///<summary>[WARNING] This variable is only available in the Editor and will not be included in the compilation of your project, in the final Build.</summary> 
        [HideInInspector]
        public string currentBoneNameRendering = "";
        ///<summary>[WARNING] This variable is only available in the Editor and will not be included in the compilation of your project, in the final Build.</summary> 
        [HideInInspector]
        public float gizmosSizeInterface = 0.01f;
        ///<summary>[WARNING] This variable is only available in the Editor and will not be included in the compilation of your project, in the final Build.</summary> 
        [HideInInspector]
        public bool renderGizmoOfBone = true;
        ///<summary>[WARNING] This variable is only available in the Editor and will not be included in the compilation of your project, in the final Build.</summary> 
        [HideInInspector]
        public bool renderLabelOfBone = true;
        ///<summary>[WARNING] This variable is only available in the Editor and will not be included in the compilation of your project, in the final Build.</summary> 
        [HideInInspector]
        public bool pingBoneOnShowVertices = false;
        ///<summary>[WARNING] This variable is only available in the Editor and will not be included in the compilation of your project, in the final Build.</summary> 
        [HideInInspector]
        public SkinnedMeshRenderer meshRendererBonesToAnimateThis = null;
        ///<summary>[WARNING] This variable is only available in the Editor and will not be included in the compilation of your project, in the final Build.</summary> 
        [HideInInspector]
        public BonesLinkingMethod bonesLinkingMethod = BonesLinkingMethod.IdenticalHierarchiesOnly;
        [HideInInspector]
        public GameObject modelBonesRoot = null;
        ///<summary>[WARNING] This variable is only available in the Editor and will not be included in the compilation of your project, in the final Build.</summary> 
        [HideInInspector]
        public bool useRootBoneToo = true;

        //The UI of this component
        #region INTERFACE_CODE
        [UnityEditor.CustomEditor(typeof(SkinnedMeshBonesManager))]
        public class CustomInspector : UnityEditor.Editor
        {
            //Private variables of editor
            private Vector2 bonesListScroll = Vector2.zero;
            private Vector3 currentPostionOfVerticesText = Vector3.zero;
            private string currentTextOfVerticesText = "";
            private int currentSelectedVertice = -1;

            public override void OnInspectorGUI()
            {
                //Start the undo event support, draw default inspector and monitor of changes
                SkinnedMeshBonesManager script = (SkinnedMeshBonesManager)target;
                script.gizmosOfThisComponentIsDisabled = MTAssetsEditorUi.DisableGizmosInSceneView("SkinnedMeshBonesManager", script.gizmosOfThisComponentIsDisabled);

                //Try to load needed assets
                Texture selectedBone = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Plugins/MT Assets/Skinned Mesh Combiner/Editor/Images/SelectedBone.png", typeof(Texture));
                Texture unselectedBone = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Plugins/MT Assets/Skinned Mesh Combiner/Editor/Images/UnselectedBone.png", typeof(Texture));
                //If fails on load needed assets, locks ui
                if (selectedBone == null || unselectedBone == null)
                {
                    EditorGUILayout.HelpBox("Unable to load required files. Please reinstall Skinned Mesh Combiner to correct this problem.", MessageType.Error);
                    return;
                }

                //Try to get prefab parent, is different from null, if this is a prefab
                var parentPrefab = PrefabUtility.GetCorrespondingObjectFromSource(script.gameObject.transform);

                //Description
                GUILayout.Space(10);
                EditorGUILayout.HelpBox("Remember to read the Skinned Mesh Combiner documentation to understand how to use it.\nGet support at: mtassets@windsoft.xyz", MessageType.None);
                GUILayout.Space(10);

                //If not exists a skinned mesh renderer or null mesh, stop this interface
                SkinnedMeshRenderer meshRenderer = script.GetComponent<SkinnedMeshRenderer>();
                if (meshRenderer == null)
                {
                    EditorGUILayout.HelpBox("A \"Skinned Mesh Renderer\" component could not be found in this GameObject. Please insert this manager into a Skinned Renderer.", MessageType.Error);
                    return;
                }
                if (meshRenderer != null && meshRenderer.sharedMesh == null)
                {
                    EditorGUILayout.HelpBox("It was not possible to find a mesh associated with the Skinned Mesh Renderer component of this GameObject. Please associate a valid mesh with this Skinned Mesh Renderer, so that you can manage the Bones.", MessageType.Error);
                    return;
                }

                //Verify if is playing
                if (Application.isPlaying == true)
                {
                    EditorGUILayout.HelpBox("The bone management interface is not available while the application is running, only the API for this component works during execution.", MessageType.Info);
                    return;
                }

                //If bone info not loaded automatically
                if (script.bonesCacheOfThisRenderer.Count == 0)
                {
                    script.bonesCacheOfThisRenderer.Clear();
                    script.UpdateBonesCacheAndGetAllBonesAndDataInList();
                }

                //Bones list
                EditorGUILayout.LabelField("All Bones Of This Mesh (" + meshRenderer.sharedMesh.name + ")", EditorStyles.boldLabel);
                GUILayout.Space(10);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("All Bones Found In This Skinned Mesh Renderer", GUILayout.Width(280));
                GUILayout.Space(MTAssetsEditorUi.GetInspectorWindowSize().x - 280);
                EditorGUILayout.LabelField("Size", GUILayout.Width(30));
                EditorGUILayout.IntField(script.bonesCacheOfThisRenderer.Count, GUILayout.Width(50));
                EditorGUILayout.EndHorizontal();
                GUILayout.BeginVertical("box");
                bonesListScroll = EditorGUILayout.BeginScrollView(bonesListScroll, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.Width(MTAssetsEditorUi.GetInspectorWindowSize().x), GUILayout.Height(250));
                if (script.bonesCacheOfThisRenderer != null)
                {
                    //If is using another bones hierarchy
                    if (script.anotherBonesHierarchyCurrentInUse != null)
                        EditorGUILayout.HelpBox("This bone hierarchy belongs to the Skinned Mesh Renderer \"" + script.anotherBonesHierarchyCurrentInUse.transform.name + "\", however, it is the bone hierarchy of \"" + script.anotherBonesHierarchyCurrentInUse.transform.name + "\" that is animating this mesh. You can still see which bones control which vertices and so on.", MessageType.Warning);

                    //If have null bones in bones hierarchy
                    if (script.bonesCacheOfThisRenderer.Count == 0)
                        EditorGUILayout.HelpBox("The bone hierarchy of this mesh is apparently corrupted. One or more bones are null, nonexistent or have been deleted. The hierarchy of bones to which this mesh is linked may also no longer exist. Try to have this mesh animated by another bone hierarchy below.", MessageType.Warning);

                    //Create style of icon
                    GUIStyle estiloIcone = new GUIStyle();
                    estiloIcone.border = new RectOffset(0, 0, 0, 0);
                    estiloIcone.margin = new RectOffset(4, 0, 6, 0);

                    foreach (BoneInfo bone in script.bonesCacheOfThisRenderer)
                    {
                        //List each bone
                        if (bone.hierarchyIndex > 0)
                            GUILayout.Space(8);
                        EditorGUILayout.BeginHorizontal();
                        if (script.boneInfoToShowVertices == null || bone.hierarchyIndex != script.boneInfoToShowVertices.hierarchyIndex)
                            GUILayout.Box(unselectedBone, estiloIcone, GUILayout.Width(24), GUILayout.Height(24));
                        if (script.boneInfoToShowVertices != null && bone.hierarchyIndex == script.boneInfoToShowVertices.hierarchyIndex)
                            GUILayout.Box(selectedBone, estiloIcone, GUILayout.Width(24), GUILayout.Height(24));
                        EditorGUILayout.BeginVertical();
                        EditorGUILayout.LabelField("(" + bone.hierarchyIndex.ToString() + ") " + bone.name, EditorStyles.boldLabel);
                        GUILayout.Space(-3);
                        EditorGUILayout.LabelField("Influencing " + bone.verticesOfThis.Count + " vertices in this mesh.");
                        EditorGUILayout.EndVertical();
                        GUILayout.Space(20);
                        EditorGUILayout.BeginVertical();
                        GUILayout.Space(6);
                        EditorGUILayout.BeginHorizontal();
                        if (GUILayout.Button("Path", GUILayout.Height(20)))
                        {
                            if (bone.gameObject != null)
                                EditorUtility.DisplayDialog("Ping to \"" + bone.name + "\" bone of this mesh", "The path of GameObject/Transform of this bone is...\n\n" + bone.transformPath, "Ok");
                            if (bone.gameObject == null)
                                EditorUtility.DisplayDialog("Bone Error", "This bone transform, not found in this scene.", "Ok");
                            EditorGUIUtility.PingObject(bone.gameObject);
                        }
                        if (script.boneInfoToShowVertices == null || bone.hierarchyIndex != script.boneInfoToShowVertices.hierarchyIndex)
                            if (GUILayout.Button("Vertices", GUILayout.Height(20)))
                                if (EditorUtility.DisplayDialog("Show Vertices", "You are about to display all the vertices affected by this bone. This can be slow depending on the grandeur of your model and how many vertices are being affected. Do you wish to continue?", "Yes", "No") == true)
                                {
                                    //Change the bone info to view vertices, and reset editor data
                                    script.boneInfoToShowVertices = bone;
                                    script.currentBoneNameRendering = "Showing vertices influenceds by bone\n\"" + bone.name + "\"\nVertices Influenceds: " + bone.verticesOfThis.Count;
                                    currentSelectedVertice = -1;
                                    currentPostionOfVerticesText = Vector3.zero;
                                    currentTextOfVerticesText = "";
                                    if (script.pingBoneOnShowVertices)
                                        EditorGUIUtility.PingObject(bone.gameObject);
                                }
                        if (script.boneInfoToShowVertices != null && bone.hierarchyIndex == script.boneInfoToShowVertices.hierarchyIndex)
                            if (GUILayout.Button("--------", GUILayout.Height(20)))
                            {
                                script.boneInfoToShowVertices = null;
                                script.currentBoneNameRendering = "";
                                currentSelectedVertice = -1;
                                currentPostionOfVerticesText = Vector3.zero;
                                currentTextOfVerticesText = "";
                            }
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        EditorGUILayout.EndHorizontal();
                        GUILayout.Space(2);
                    }
                }
                EditorGUILayout.EndScrollView();
                GUILayout.EndVertical();

                EditorGUILayout.HelpBox("" +
                   "In the list above, you can see all the bones that are linked to the mesh of this Skinned Mesh Renderer. You can also see the bone hierarchy that this mesh is using. All bones listed above are linked to this mesh and may or may not deform vertices of this mesh.", MessageType.Info);

                script.gizmosSizeInterface = EditorGUILayout.Slider(new GUIContent("Gizmos Size In Interface",
                         "The size that the Gizmos will be rendered in interface of this component."),
                         script.gizmosSizeInterface, 0.001f, 0.1f);

                script.renderGizmoOfBone = (bool)EditorGUILayout.Toggle(new GUIContent("Render Gizmo Of Bone",
                        "Render gizmo of bone selected to show vertices?"),
                        script.renderGizmoOfBone);

                script.renderLabelOfBone = (bool)EditorGUILayout.Toggle(new GUIContent("Render Label Of Bone",
                       "Render label of bone selected to show vertices?"),
                       script.renderLabelOfBone);

                script.pingBoneOnShowVertices = (bool)EditorGUILayout.Toggle(new GUIContent("Ping Bone On Show Vert.",
                      "Ping/Highlight bone transform in scene, everytime that you show vertices of the bone?"),
                      script.pingBoneOnShowVertices);

                GUILayout.Space(10);
                if (GUILayout.Button("Update And Show Info About Bones Hierarchy Of This Mesh", GUILayout.Height(40)))
                {
                    script.bonesCacheOfThisRenderer.Clear();
                    script.UpdateBonesCacheAndGetAllBonesAndDataInList();
                    Debug.Log("The information on the bone hierarchy of this mesh has been updated.");
                }
                GUILayout.Space(10);

                //Bones mangement
                GUILayout.Space(10);
                EditorGUILayout.LabelField("Use Another Bone Hierarchy To Animate This", EditorStyles.boldLabel);
                GUILayout.Space(10);

                if (script.anotherBonesHierarchyCurrentInUse == null)
                {
                    if (script.meshRendererBonesToAnimateThis != null && parentPrefab != null)
                        EditorGUILayout.HelpBox("It looks like this GameObject is a prefab, or part of one. Therefore, this component may not be able to place this mesh, next to the bone hierarchy when you make the switch, for the reason that Unity does not allow the reorganization of GameObjects in prefabs. When you make the switch so that this mesh is animated by another bone hierarchy, everything will work normally, however, you will need to organize this GameObject manually.", MessageType.Warning);

                    //If not provided the skinned mesh renderer
                    if (script.meshRendererBonesToAnimateThis == null)
                        EditorGUILayout.HelpBox("Provide a other Skinned Mesh Renderer so that its bones hierarchy will be used to animate this mesh. This can be useful for separated FBX files follow the other Skinned Mesh Renderer animation, for example.", MessageType.Info);

                    script.meshRendererBonesToAnimateThis = (SkinnedMeshRenderer)EditorGUILayout.ObjectField(new GUIContent("Bones Hierarchy To Use",
                        "Provide another Skinned Mesh Renderer. This mesh will have the bones of the other Skinned Mesh Renderer linked and the bones of the other renderer will animate this mesh."),
                        script.meshRendererBonesToAnimateThis, typeof(SkinnedMeshRenderer), true, GUILayout.Height(16));
                    if (script.meshRendererBonesToAnimateThis != null)
                    {
                        script.bonesLinkingMethod = (BonesLinkingMethod)EditorGUILayout.EnumPopup(new GUIContent("Bones Linking Method",
                            "Defines the method that will be used to link the bones of the other Skinned Mesh Renderer to this mesh...\n\nIdentical Hierarchies Only - With this method, the bones of the other renderer will only be linked to this mesh if the Hierarchy of Bones of this and the other mesh are IDENTICAL.\n\nIdentical Bones Only - With this method, only bones from the other renderer that are IDENTICAL to each bone in this mesh will be linked. With this method, the two Hierarchies of Bones not need to be identical, however, they must have the same bones."),
                            script.bonesLinkingMethod);
                        if (script.bonesLinkingMethod == BonesLinkingMethod.IdenticalBonesOnly)
                        {
                            EditorGUI.indentLevel += 1;
                            script.modelBonesRoot = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Model Bones Root",
                                "You must inform for this parameter, the GameObject that contains all GameObjects of bones of your character or model. If a bone is not found in the target renderer, it will be looked for in the model's or character's bones GameObjects.\n\nThis parameter only needs to be informed if you use the \"Identical Bones Only\" linking method."),
                                script.modelBonesRoot, typeof(GameObject), true, GUILayout.Height(16));
                            EditorGUI.indentLevel -= 1;
                        }

                        script.useRootBoneToo = (bool)EditorGUILayout.Toggle(new GUIContent("Use Root Bone Too",
                            "Use the same root bone of the target Skinned Mesh Renderer too?"),
                            script.useRootBoneToo);

                        GUILayout.Space(10);

                        ValidationStatus validationStatus = script.isValidTargetSkinnedMeshRendererBonesHierarchy(script.meshRendererBonesToAnimateThis, script.bonesLinkingMethod, script.modelBonesRoot, false);
                        if (validationStatus.isValid == false)
                            EditorGUILayout.HelpBox(validationStatus.information, MessageType.Error);
                        if (validationStatus.isValid == true)
                            if (GUILayout.Button("Use Bones From That Skinned Mesh Renderer", GUILayout.Height(40)))
                                if (EditorUtility.DisplayDialog("Continue?", "After performing this action, the mesh of this Skinned Mesh Renderer will be animated using the bones of the other Skinned Mesh Renderer that you provided.\n\nYou will no longer be able to undo this change. To obtain the Skinned Mesh Renderer and all its original information, it will be necessary to re-add this mesh to your scene again.", "Continue", "Cancel") == true)
                                    script.UseAnotherBoneHierarchyForAnimateThis(script.meshRendererBonesToAnimateThis, script.bonesLinkingMethod, script.modelBonesRoot, script.useRootBoneToo);
                    }
                }
                if (script.anotherBonesHierarchyCurrentInUse != null)
                    EditorGUILayout.HelpBox("This Skinned Mesh Renderer has already been linked to be animated by the Skinned Mesh Renderer \"" + script.anotherBonesHierarchyCurrentInUse.gameObject.name + "\" Bones hierarchy. This action cannot be undone!", MessageType.Info);

                GUILayout.Space(10);
            }

            protected virtual void OnSceneGUI()
            {
                SkinnedMeshBonesManager script = (SkinnedMeshBonesManager)target;

                //Get this mesh renderer
                SkinnedMeshRenderer meshRenderer = script.GetComponent<SkinnedMeshRenderer>();

                //If not have components to worl
                if (meshRenderer == null || meshRenderer.sharedMesh == null)
                    return;

                //Render only if have a boneinfo to render
                if (meshRenderer != null && meshRenderer.sharedMesh != null && script.boneInfoToShowVertices == null)
                    return;

                EditorGUI.BeginChangeCheck();
                Undo.RecordObject(target, "Undo Event");

                //Set the base color of gizmos
                Handles.color = Color.green;

                //Render each vertice if, bone info to show vertices is not null
                foreach (VerticeData vertice in script.boneInfoToShowVertices.verticesOfThis)
                {
                    //Color the gizmo according the weight
                    Handles.color = Color.Lerp(Color.green, Color.red, vertice.weightOfInfluencer);

                    //If is the selected vertice
                    if (vertice.indexOfThisVerticeInMesh == currentSelectedVertice)
                        Handles.color = Color.white;

                    //Draw current vertice
                    Vector3 currentVertice = script.transform.TransformPoint(meshRenderer.sharedMesh.vertices[vertice.indexOfThisVerticeInMesh]);
                    Vector3 currentVerticeScaled = new Vector3(currentVertice.x * script.transform.localScale.x, currentVertice.y * script.transform.localScale.y, currentVertice.z * script.transform.localScale.z);
                    if (Handles.Button(currentVertice, Quaternion.identity, script.gizmosSizeInterface, script.gizmosSizeInterface, Handles.SphereHandleCap))
                    {
                        currentPostionOfVerticesText = currentVertice;
                        currentTextOfVerticesText = "Vertice Index: " + vertice.indexOfThisVerticeInMesh + "/" + meshRenderer.sharedMesh.vertices.Length + "\nInfluencer Bone: " + vertice.influencerBone.name + "\nWeight of Influence: " + vertice.weightOfInfluencer.ToString("F2");
                        currentSelectedVertice = vertice.indexOfThisVerticeInMesh;
                    }
                }

                //Prepare the text
                GUIStyle styleVerticeDetail = new GUIStyle();
                styleVerticeDetail.normal.textColor = Color.white;
                styleVerticeDetail.alignment = TextAnchor.MiddleCenter;
                styleVerticeDetail.fontStyle = FontStyle.Bold;
                styleVerticeDetail.contentOffset = new Vector2(-currentTextOfVerticesText.Substring(0, currentTextOfVerticesText.IndexOf("\n") + 1).Length * 1.8f, 30);

                //Draw the vertice text, if is desired
                if (currentPostionOfVerticesText != Vector3.zero)
                    Handles.Label(currentPostionOfVerticesText, currentTextOfVerticesText, styleVerticeDetail);

                //Render the bone, if is desired
                if (script.renderGizmoOfBone)
                {
                    Handles.color = Color.gray;
                    if (script.boneInfoToShowVertices.transform != null)
                        Handles.ArrowHandleCap(0, script.boneInfoToShowVertices.transform.position, Quaternion.identity * script.boneInfoToShowVertices.transform.rotation * Quaternion.AngleAxis(90, Vector3.left), script.gizmosSizeInterface * 18f, EventType.Repaint);
                }

                //Render the bone name, if is desired
                if (script.renderLabelOfBone)
                {
                    GUIStyle styleBoneName = new GUIStyle();
                    styleBoneName.normal.textColor = Color.white;
                    styleBoneName.alignment = TextAnchor.MiddleCenter;
                    styleBoneName.fontStyle = FontStyle.Bold;
                    styleBoneName.contentOffset = new Vector2(-script.currentBoneNameRendering.Substring(0, script.currentBoneNameRendering.IndexOf("\n") + 1).Length * 1.5f, 30);
                    if (string.IsNullOrEmpty(script.currentBoneNameRendering) == false)
                        Handles.Label(script.boneInfoToShowVertices.transform.position, script.currentBoneNameRendering, styleBoneName);
                }

                //Apply changes on script, case is not playing in editor
                if (GUI.changed == true && Application.isPlaying == false)
                {
                    EditorUtility.SetDirty(script);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(script.gameObject.scene);
                }
                if (EditorGUI.EndChangeCheck() == true)
                {
                    //Apply the change, if moved the handle
                    //script.transform.position = teste;
                }
                Repaint();
            }
        }

        private string GetGameObjectPath(Transform transform)
        {
            //Return the full path of a GameObject
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }
            return path;
        }

        private void UpdateBonesCacheAndGetAllBonesAndDataInList()
        {
            //Only update the bones cache of this renderer, if this renderer is updated
            if (bonesCacheOfThisRenderer.Count > 0)
                return;

            //Get the skinned mesh renderer
            SkinnedMeshRenderer meshRender = GetComponent<SkinnedMeshRenderer>();

            //Start the scan
            if (meshRender != null && meshRender.sharedMesh != null)
            {
                //Get all bones
                Transform[] allBonesTransform = meshRender.bones;

                //If have a null bone, stop the process
                foreach (Transform transform in allBonesTransform)
                    if (transform == null)
                    {
                        bonesCacheOfThisRenderer.Clear();
                        return;
                    }

                //Create all boneinfo
                for (int i = 0; i < allBonesTransform.Length; i++)
                {
                    BoneInfo boneInfo = new BoneInfo();
                    boneInfo.transform = allBonesTransform[i];
                    boneInfo.name = allBonesTransform[i].name;
                    boneInfo.gameObject = allBonesTransform[i].transform.gameObject;
                    boneInfo.transformPath = GetGameObjectPath(allBonesTransform[i]);
                    boneInfo.hierarchyIndex = i;

                    bonesCacheOfThisRenderer.Add(boneInfo);
                }

                //Associate each vertice influenced by each bone to respective key
                for (int i = 0; i < meshRender.sharedMesh.vertexCount; i++)
                {
                    //Verify if exists a weight of a possible bone X influencing this vertice. Create a vertice data that stores and link this vertice inside your respective BoneInfo
                    if (meshRender.sharedMesh.boneWeights[i].weight0 > 0)
                    {
                        int boneIndexOfInfluencerBoneOfThisVertice = meshRender.sharedMesh.boneWeights[i].boneIndex0;
                        bonesCacheOfThisRenderer[boneIndexOfInfluencerBoneOfThisVertice].verticesOfThis.Add(new VerticeData(bonesCacheOfThisRenderer[boneIndexOfInfluencerBoneOfThisVertice], meshRender.sharedMesh.boneWeights[i].weight0, i));
                    }
                    if (meshRender.sharedMesh.boneWeights[i].weight1 > 0)
                    {
                        int boneIndexOfInfluencerBoneOfThisVertice = meshRender.sharedMesh.boneWeights[i].boneIndex1;
                        bonesCacheOfThisRenderer[boneIndexOfInfluencerBoneOfThisVertice].verticesOfThis.Add(new VerticeData(bonesCacheOfThisRenderer[boneIndexOfInfluencerBoneOfThisVertice], meshRender.sharedMesh.boneWeights[i].weight1, i));
                    }
                    if (meshRender.sharedMesh.boneWeights[i].weight2 > 0)
                    {
                        int boneIndexOfInfluencerBoneOfThisVertice = meshRender.sharedMesh.boneWeights[i].boneIndex2;
                        bonesCacheOfThisRenderer[boneIndexOfInfluencerBoneOfThisVertice].verticesOfThis.Add(new VerticeData(bonesCacheOfThisRenderer[boneIndexOfInfluencerBoneOfThisVertice], meshRender.sharedMesh.boneWeights[i].weight2, i));
                    }
                    if (meshRender.sharedMesh.boneWeights[i].weight3 > 0)
                    {
                        int boneIndexOfInfluencerBoneOfThisVertice = meshRender.sharedMesh.boneWeights[i].boneIndex3;
                        bonesCacheOfThisRenderer[boneIndexOfInfluencerBoneOfThisVertice].verticesOfThis.Add(new VerticeData(bonesCacheOfThisRenderer[boneIndexOfInfluencerBoneOfThisVertice], meshRender.sharedMesh.boneWeights[i].weight3, i));
                    }
                }
            }
        }
        #endregion
#endif

        //Private methods for this component Interface and API.

        private Transform FindChildRecursive(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName)
                {
                    return child;
                }
                else
                {
                    Transform found = FindChildRecursive(child, childName);
                    if (found != null)
                    {
                        return found;
                    }
                }
            }
            return null;
        }

        private ValidationStatus isValidTargetSkinnedMeshRendererBonesHierarchy(SkinnedMeshRenderer targetMeshRenderer, BonesLinkingMethod bonesLinkingMethod, GameObject modelBonesRoot, bool launchLogs)
        {
            //Prepare the value
            bool isValid = true;
            //Prepare the possible error message
            string possibleErrorMessage = "";

            //Get this mesh renderer
            SkinnedMeshRenderer thisMeshRenderer = GetComponent<SkinnedMeshRenderer>();

            //** START OF THE VALIDATION **/

            //Validate
            if (thisMeshRenderer == null)
            {
                isValid = false;
                possibleErrorMessage = "Could not find a Skinned Mesh Renderer component in the GameObject of the Skinned Mesh Bones Manager.";
            }
            if (targetMeshRenderer == null)
            {
                isValid = false;
                possibleErrorMessage = "A target Skinned Mesh Renderer was not provided!";
            }
            if (modelBonesRoot == null && bonesLinkingMethod == BonesLinkingMethod.IdenticalBonesOnly)
            {
                isValid = false;
                possibleErrorMessage = "When using the \"Identical Bones Only\" linking method, you need to provide the root GameObject that contains all of your character bones GameObjects.";
            }

            //Validate for method "Identical Hierarchies Only"
            if (thisMeshRenderer != null && targetMeshRenderer != null)
                if (bonesLinkingMethod == BonesLinkingMethod.IdenticalHierarchiesOnly)
                    if (thisMeshRenderer.bones.Length != targetMeshRenderer.bones.Length)
                    {
                        isValid = false;
                        string finalDebugString = "Bones hierarchy of this renderer...";
                        if (thisMeshRenderer.bones.Length == 0)
                            finalDebugString += "\n- None";
                        for (int i = 0; i < thisMeshRenderer.bones.Length; i++)
                            finalDebugString += "\n- " + thisMeshRenderer.bones[i].name;
                        if (thisMeshRenderer.bones.Length > 0)
                            finalDebugString += "\n- [" + thisMeshRenderer.bones.Length + " Bones]";
                        finalDebugString += "\n\nBones hierarchy of other renderer...";
                        if (targetMeshRenderer.bones.Length == 0)
                            finalDebugString += "\n- None";
                        for (int i = 0; i < targetMeshRenderer.bones.Length; i++)
                            finalDebugString += "\n- " + targetMeshRenderer.bones[i].name;
                        if (targetMeshRenderer.bones.Length > 0)
                            finalDebugString += "\n- [" + targetMeshRenderer.bones.Length + " Bones]";
                        possibleErrorMessage = "It is not possible for this Skinned Mesh Renderer to use the Skinned Mesh Renderer \"" + targetMeshRenderer.gameObject.name + "\" bone hierarchy, as the two hierarchies are not identical. Both Skinned Mesh Renderers must have an identical bone hierarchy to make it possible for this mesh to be animated by the bones of the desired Skinned Mesh Renderer.\n\n" + finalDebugString;
                    }
            //Validate for method "Identical Hierarchies Only"
            if (thisMeshRenderer != null && targetMeshRenderer != null && modelBonesRoot != null)
                if (bonesLinkingMethod == BonesLinkingMethod.IdenticalBonesOnly)
                {
                    List<string> notFoundBonesInTargetMesh = new List<string>();
                    List<Transform> newBonesList = new List<Transform>();
                    for (int i = 0; i < thisMeshRenderer.bones.Length; i++)
                    {
                        bool foundIdenticalBoneInTargetRenderer = false;
                        for (int x = 0; x < targetMeshRenderer.bones.Length; x++)
                        {
                            Transform thisRendererBone = thisMeshRenderer.bones[i];
                            Transform targetRendererBone = targetMeshRenderer.bones[x];
                            if (thisRendererBone == null)
                                break;
                            if (thisRendererBone != null && targetRendererBone != null)
                                if (thisMeshRenderer.bones[i].gameObject.name == targetMeshRenderer.bones[x].gameObject.name)
                                {
                                    newBonesList.Add(targetMeshRenderer.bones[x]);
                                    foundIdenticalBoneInTargetRenderer = true;
                                }
                        }
                        if (foundIdenticalBoneInTargetRenderer == false && thisMeshRenderer.bones[i] != null)
                        {
                            Transform boneFoundInRootBone = FindChildRecursive(modelBonesRoot.transform, thisMeshRenderer.bones[i].name);
                            if (boneFoundInRootBone != null)
                                newBonesList.Add(boneFoundInRootBone);
                            if (boneFoundInRootBone == null)
                                notFoundBonesInTargetMesh.Add(thisMeshRenderer.bones[i].name);
                        }
                    }
                    int validBonesInOriginalMesh = 0;
                    for (int i = 0; i < thisMeshRenderer.bones.Length; i++)
                        if (thisMeshRenderer.bones[i] != null)
                            validBonesInOriginalMesh += 1;
                    if (validBonesInOriginalMesh != newBonesList.Count)
                    {
                        isValid = false;
                        string finalStrBonesNotFound = "";
                        if (notFoundBonesInTargetMesh.Count == 0)
                            finalStrBonesNotFound = "- None";
                        if (notFoundBonesInTargetMesh.Count > 0)
                            for (int i = 0; i < notFoundBonesInTargetMesh.Count; i++)
                            {
                                if (i != 0)
                                    finalStrBonesNotFound += "\n";
                                finalStrBonesNotFound += ("- " + notFoundBonesInTargetMesh[i]);
                            }
                        possibleErrorMessage = "It was not possible to find all the identical bones of this Skinned Mesh Renderer present in the other provided Skinned Mesh Renderer. Apparently the other Skinned Mesh Renderer has a bone hierarchy that is not the same as this mesh bone hierarchy.\n\nBones names not found in the Model or in target Skinned Mesh Renderer...\n" + finalStrBonesNotFound;
                    }
                }

            //**  END OF THE VALIDATION  **/

            //Launch logs if is desired
            if (launchLogs == true)
            {
#if !UNITY_EDITOR
                if (isValid == false)
                    Debug.Log(possibleErrorMessage);
#endif
#if UNITY_EDITOR
                if (isValid == false && Application.isPlaying == false)
                    EditorUtility.DisplayDialog("Error", possibleErrorMessage, "Ok");
                if (isValid == false && Application.isPlaying == true)
                    Debug.Log(possibleErrorMessage);
#endif
            }

            //Return
            return new ValidationStatus(isValid, possibleErrorMessage);
        }

        //Public API methods

        public bool UseAnotherBoneHierarchyForAnimateThis(SkinnedMeshRenderer meshRendererToUse, BonesLinkingMethod bonesLinkingMethod, GameObject modelBonesRoot, bool useRootBoneToo)
        {
            //First validate the target skinned mesh renderer
            if (isValidTargetSkinnedMeshRendererBonesHierarchy(meshRendererToUse, bonesLinkingMethod, modelBonesRoot, true).isValid == false)
                return false;

            //Get needed components reference
            SkinnedMeshRenderer thisMeshRenderer = this.gameObject.GetComponent<SkinnedMeshRenderer>();
            SkinnedMeshRenderer targetRenderer = meshRendererToUse;

            //If is desired to use the "IDENTICAL HIERARCHIES ONLY"...
            if (bonesLinkingMethod == BonesLinkingMethod.IdenticalHierarchiesOnly)
            {
                //Make this mesh renderer use the bones array of the target renderer
                thisMeshRenderer.bones = targetRenderer.bones;
            }
            //If is desired to use the "IDENTICAL BONES ONLY"...
            if (bonesLinkingMethod == BonesLinkingMethod.IdenticalBonesOnly)
            {
                //Build a new bones array using only the bones of the target renderer that is identical to bones of this mesh
                Transform[] newBonesArray = new Transform[thisMeshRenderer.bones.Length];
                for (int i = 0; i < thisMeshRenderer.bones.Length; i++)
                {
                    //Prepare information if has found a bone identical to this in the target renderer
                    bool foundIdenticalBoneInTargetRenderer = false;

                    for (int x = 0; x < targetRenderer.bones.Length; x++)
                    {
                        //Get the bone of this mesh and of the target mesh
                        Transform thisRendererBone = thisMeshRenderer.bones[i];
                        Transform targetRendererBone = targetRenderer.bones[x];

                        //If the current bone of this render is null, stop searching a identical bone in the other renderer
                        if (thisRendererBone == null)
                            break;

                        //If the current bone of this renderer, and the current bone of the other renderer have same name, insert the bone of the other renderer in "new bones" array, in same slot of the old bone
                        if (thisRendererBone != null && targetRendererBone != null)
                            if (thisRendererBone.gameObject.name == targetRendererBone.gameObject.name)
                                newBonesArray[i] = targetRenderer.bones[x];
                    }

                    //If not found a identical bone to this in the target renderer, try to find in the hierarchy of GameObjects of bones
                    if (foundIdenticalBoneInTargetRenderer == false && thisMeshRenderer.bones[i] != null)
                    {
                        Transform boneFoundInModelBones = FindChildRecursive(modelBonesRoot.transform, thisMeshRenderer.bones[i].name);
                        if (boneFoundInModelBones != null)
                            newBonesArray[i] = boneFoundInModelBones;
                    }
                }
                thisMeshRenderer.bones = newBonesArray;
            }

            //If is desired to use the same root bone...
            if (useRootBoneToo == true)
                thisMeshRenderer.rootBone = targetRenderer.rootBone;

            //Set the stats
            anotherBonesHierarchyCurrentInUse = targetRenderer;
            //Move this gameobject to be child of the parent of target Skinned Mesh Renderer
            this.gameObject.transform.parent = targetRenderer.transform.parent;

            //Return true for success
            return true;
        }
    }
}