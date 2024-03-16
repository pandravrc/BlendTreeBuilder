using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System;
using System.IO;
using System.Collections.Generic;
public class PBlendTreeBuilder
{
    private BlendTree[] buildingTrees;
    private float[] defaultValues;
    public string projectFolder;
    public int nestingTreeNum = 0;
    private string nextName;
    public List<string> animationParameters = new List<string>();
    public PBlendTreeBuilder(string _projectFolder)
    {
        buildingTrees = new BlendTree[1];
        defaultValues = new float[1];
        projectFolder = $@"{_projectFolder.Replace("\\", "/")}/";
        projectFolder = projectFolder.Replace("//", "/").Replace("/Gen/", "/").Replace("/Editor/", "/").Replace("/Res/", "/").Replace("/ReSource/", "/");
        projectFolder = getAssetsPath(projectFolder);
    }
    public BlendTree getTree(int n)
    {
        return buildingTrees[n];
    }
    public int maxTreeNum()
    {
        return buildingTrees.Length - 1;
    }
    public void make(string name = null)
    {
        Array.Resize(ref buildingTrees, buildingTrees.Length + 1);
        Array.Resize(ref defaultValues, buildingTrees.Length + 1);
        int newTreeNum = maxTreeNum();
        var makingTree = new BlendTree();
        buildingTrees[newTreeNum] = makingTree;
        makingTree.useAutomaticThresholds = false;
        defaultValues[newTreeNum] = 0;
        if (name != null)
        {
            makingTree.name = name;
        }
        else
        {
            if (newTreeNum == 1)
            {
                makingTree.name = "DBT";
                defaultValues[1] = 1f;
            }
            else
            {
                makingTree.name = $@"T{newTreeNum}";
            }
        }
    }
    public void save(string GenFolderPath, bool clear = true)
    {
        if (clear)
        {
            PAssetsSave.run(GenFolderPath, true);
        }
        for (int n = maxTreeNum(); n > 0; n--)
        {
            PAssetsSave.run(GenFolderPath, false, buildingTrees[n]);
        }
    }
    public float defaultValue(int _treeNum = -1)
    {
        return defaultValues[_treeNum];
    }
    public string blendParameter(int _treeNum = -1)
    {
        return getTree(_treeNum).blendParameter;
    }
    public BlendTree currentTree()
    {
        return buildingTrees[nestingTreeNum];
    }
    public void add1D(float threshold, string childThresholdParamName, Action act) // Parent 1D Child 1D
    {
        if (currentTree().blendType != BlendTreeType.Simple1D) Debug.LogError("Type Mismatch Error: Different input provided to method expecting parent type 1D.");
        addBrendTree(BlendTreeType.Simple1D, threshold, null, childThresholdParamName, act);
    }
    public void add1D(string directParameterName, string childThresholdParamName, Action act) //Parent Direct Child 1D
    {
        if (currentTree().blendType != BlendTreeType.Direct) Debug.LogError("Type Mismatch Error: Different input provided to method expecting parent type Direct.");
        addBrendTree(BlendTreeType.Simple1D, 0, directParameterName, childThresholdParamName, act);
    }
    public void addDirect(float threshold, Action act = null) // Parent 1D Child Direct
    {
        if (currentTree().blendType != BlendTreeType.Simple1D) Debug.LogError("Type Mismatch Error: Different input provided to method expecting parent type 1D.");
        addBrendTree(BlendTreeType.Direct, threshold, null, null, act);
    }
    public void addDirect(string directParameterName, Action act = null) // Parent Direct Child Direct
    {
        if (currentTree().blendType != BlendTreeType.Direct) Debug.LogError("Type Mismatch Error: Different input provided to method expecting parent type Direct.");
        addBrendTree(BlendTreeType.Direct, 0, directParameterName, null, act);
    }
    public void rootDBT(Action act = null)
    {
        addBrendTree(BlendTreeType.Direct, 0, "ONEf", null, act);
    }
    public void addBrendTree(BlendTreeType Type, float threshold, string directParameterName, string childThresholdParamName, Action act)
    {
        int onEnterTreeNum = nestingTreeNum;
        make(null);
        int nowChildTreeNum = maxTreeNum();
        nestingTreeNum = nowChildTreeNum;
        if (nextName != null)
        {
            getTree(nowChildTreeNum).name = nextName;
            nextName = null;
        }
        getTree(nowChildTreeNum).blendType = Type;
        if (Type == BlendTreeType.Simple1D)
        {
            childThresholdParamName = normalizedParameterName(childThresholdParamName);
            getTree(nowChildTreeNum).blendParameter = childThresholdParamName;
        }
        if (act != null) act();
        nestingTreeNum = onEnterTreeNum;
        if (onEnterTreeNum >= 1)
        {
            if (getTree(onEnterTreeNum).blendType == BlendTreeType.Simple1D)
            {
                getTree(onEnterTreeNum).AddChild(getTree(nowChildTreeNum), threshold);
            }
            else if (getTree(onEnterTreeNum).blendType == BlendTreeType.Direct)
            {
                directParameterName = normalizedParameterName(directParameterName);
                getTree(onEnterTreeNum).AddDirectChild(getTree(nowChildTreeNum), directParameterName);
            }
        }
    }
    public void addMotion(float threshold, string motionPath) // Parent 1D Child Motion
    {
        if (currentTree().blendType != BlendTreeType.Simple1D) Debug.LogError("Type Mismatch Error: Different input provided to method expecting parent type 1D.");
        addMotion(threshold, null, motionPath);
    }
    public void addMotion(string directParameterName, string motionPath) // Parent Direct Child Motion
    {
        if (currentTree().blendType != BlendTreeType.Direct) Debug.LogError("Type Mismatch Error: Different input provided to method expecting parent type Direct.");
        addMotion(0, directParameterName, motionPath);
    }
    public void addMotion(float threshold, string directParameterName, string motionPath)
    {
        if (currentTree().blendType == BlendTreeType.Simple1D)
        {
            currentTree().AddChild(LoadMotion(motionPath), threshold);
        }
        else if (currentTree().blendType == BlendTreeType.Direct)
        {
            directParameterName = normalizedParameterName(directParameterName);
            currentTree().AddDirectChild(LoadMotion(motionPath), directParameterName);
        }
    }
    private string normalizedParameterName(string parameterName)
    {
        if (string.IsNullOrEmpty(parameterName))　throw new NotImplementedException("ParameterName is Null");
        string res;
        if (parameterName == "1" || parameterName == "ONEf")
        {
            res = "PBTB_CONST_1";
        }
        else if (parameterName.Contains("PBTB_CONST_"))
        {
            res = parameterName;
        }
        else
        {
            res = $@"{projectName()}_{parameterName}";
        }
        addAnimationParameters(res);
        return res;
    }
    private string normalizedMotionPath(string motionPath)
    {
        motionPath = motionPath.Trim().Replace("\\", "/");
        if (!motionPath.Contains("/")) motionPath = $@"{projectFolder}/Res/{motionPath}";
        motionPath = $@"{motionPath}.anim".Replace(".anim.anim", ".anim");
        return getAssetsPath(motionPath);
    }
    public string getAssetsPath(string path)
    {
        int assetsIndex = path.IndexOf("Assets");
        path = path.Substring(assetsIndex);
        return path;
    }
    public AnimationClip LoadMotion(string path)
    {
        path = normalizedMotionPath(path);
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("Animation clip path is empty.");
            return null;
        }
        if (!File.Exists(path))
        {
            Debug.LogError("Animation clip does not exist: " + path);
            return null;
        }
        if (!path.EndsWith(".anim"))
        {
            Debug.LogError("Animation clip file format is invalid: " + path);
            return null;
        }
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            Debug.LogError("Failed to load animation clip: " + path);
            return null;
        }
        return clip;
    }
    public string projectName()
    {
        string t = projectFolder.Replace("Assets/", "").Replace("/", "_");
        t = t.Substring(0, t.Length - 1);
        return t;
    }
    public void animatorMake()
    {
        PAnimatorMaker.run(this);
    }
    public void addAnimationParameters(string input)
    {
        if (!animationParameters.Contains(input))
        {
            animationParameters.Add(input);
        }
    }
    public PBlendTreeBuilder nName(string _nextName)
    {
        nextName = _nextName;
        return this;
    }
}
public static class PAssetsSave
{
    /// <summary>Pan Assets Save [[WARNING: This code will delete all files in the Gen folder without prior notice.]]</summary>
    private static UnityEngine.Object[] assets;
    private static UnityEngine.Object workingAsset;
    private static string workingDirectory;
    public static void run(string _workingDirectory, bool clearDirectory, params UnityEngine.Object[] _assets)
    {
        workingDirectory = _workingDirectory;
        if (clearDirectory) clearWorkDirectory();
        assets = _assets;
        createWorkDirectory();
        saveAssets();
    }
    private static string fileName()
    {
        return $@"{workingAsset.name}.{extension()}";
    }
    private static string savePath()
    {
        return $@"{workingDirectory}{fileName()}";//.Replace("(WD On)", "");
    }
    private static void clearWorkDirectory()
    {
        if (Directory.Exists(workingDirectory))
        {
            Directory.Delete(workingDirectory, true);
        }
    }
    private static void createWorkDirectory()
    {
        if (!Directory.Exists(workingDirectory))
        {
            Directory.CreateDirectory(workingDirectory);
            AssetDatabase.Refresh();
        }
    }
    private static void saveAssets()
    {
        foreach (var ast in assets)
        {
            workingAsset = ast;
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(savePath()) != null) Debug.LogWarning($@"Collision occurred in {savePath()}");
            AssetDatabase.CreateAsset(workingAsset, savePath());
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
            Debug.Log($@"Asset Saved at: {savePath()}");
        }
    }
    private static Dictionary<Type, string> extensionMap = new Dictionary<Type, string>
    {
        { typeof(BlendTree), "asset" },
        { typeof(AnimatorController), "controller" },
    };
    private static string extension()
    {
        Type astType = workingAsset.GetType();
        if (extensionMap.ContainsKey(astType)) return extensionMap[astType];
        else return "asset";
    }
}
public static class PBlendTreeExtensions
{
    public static void AddDirectChild(this BlendTree blendTree, Motion childTree, string parameterName = "ONEf")
    {
        if (blendTree == null)
        {
            throw new ArgumentNullException("blendTree not found");
        }
        if (childTree == null)
        {
            throw new ArgumentNullException("childTree not found");
        }
        if (string.IsNullOrEmpty(parameterName))
        {
            throw new ArgumentNullException("parameterName not found");
        }
        blendTree.AddChild(childTree);
        var c = blendTree.children;
        c[c.Length - 1].directBlendParameter = parameterName;
        blendTree.children = c;
    }
}
public static class PAnimatorMaker
{
    private static AnimatorController animatorController;
    private static AnimatorState animatorState;
    private static AnimatorStateMachine animatorStateMachine;
    private static string GenFolderPath;
    private static string projectName;
    public static void run(PBlendTreeBuilder bb)
    {
        animatorState = new AnimatorState()
        {
            writeDefaultValues = true,
            motion = bb.getTree(1),
            name = "DBT(WD On)",
        };
        animatorStateMachine = new AnimatorStateMachine()
        {
            name = bb.projectName(),
            states = new[]
            {
                new ChildAnimatorState
                {
                    state = animatorState,
                    position = Vector3.zero,
                }
            },
            defaultState = animatorState,
        };
        animatorController = new AnimatorController()
        {
            name = bb.projectName(),
            layers = new[]
            {
                new AnimatorControllerLayer
                {
                    blendingMode = AnimatorLayerBlendingMode.Override,
                    defaultWeight = 1,
                    name = $@"DBT_{bb.projectName()}",
                    stateMachine = animatorStateMachine
                }
            },
        };
        bb.animationParameters.Sort();
        foreach (var p in bb.animationParameters)
        {
            float defaultFloat;
            if (p.Contains("PBTB_CONST_"))
            {
                defaultFloat = float.Parse(p.Replace("PBTB_CONST_", ""));
            }
            else
            {
                defaultFloat = 0;
            }
            animatorController.AddParameter(
                new AnimatorControllerParameter()
                {
                    name = $@"{p}",
                    type = AnimatorControllerParameterType.Float,
                    defaultFloat = defaultFloat,
                }
            );
        }
        GenFolderPath = $@"{bb.projectFolder}Gen/";
        bb.save(GenFolderPath);
        PAssetsSave.run(GenFolderPath, false, animatorState, animatorStateMachine, animatorController);
        // [Warning!!] Save the smallest element first. If not, it will break when restart.
    }
}
