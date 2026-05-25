using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

public class TopicSetupWindow : EditorWindow
{
    const string KEY_TOPIC = "MQJ_Topic_Name";
    const string KEY_GRADE = "MQJ_Grade_Index";
    const string KEY_SUBJECT = "MQJ_Subject_Index";
    const string KEY_URL = "MQJ_URL";


    string topicName = "";
    int gradeIndex = 0;
    int subjectIndex = 0;

    string[] grades = { "G1", "G2", "G3", "G4", "G5", "G6", "G7", "G8", "G9", "G10", "G11", "G12" };
    string[] subjects = { "Maths", "Science", "EVS" };

    string url = "";

    [MenuItem("Tools/MQJ Topic Setup")]
    public static void ShowWindow()
    {
        var window = GetWindow<TopicSetupWindow>();
        window.titleContent = new GUIContent("MQJ Topic Setup", EditorGUIUtility.IconContent("CustomTool").image);
    }

    void OnEnable()
    {
        topicName = EditorPrefs.GetString(KEY_TOPIC, "");
        gradeIndex = EditorPrefs.GetInt(KEY_GRADE, 0);
        subjectIndex = EditorPrefs.GetInt(KEY_SUBJECT, 0);
        url = EditorPrefs.GetString(KEY_URL, "");
    }

    void OnGUI()
    {
        GUILayout.Space(10);

        // ====== HEADING ======
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
        headerStyle.alignment = TextAnchor.MiddleCenter;
        headerStyle.fontSize = 16;
        EditorGUILayout.LabelField("MQJ Topic Setup Tool", headerStyle, GUILayout.Height(30));

        GUILayout.Space(15);

        // ====== MAIN PANEL WITH BORDER ======
        EditorGUILayout.BeginVertical("box");
        GUILayout.Space(5);

        // Inputs
        EditorGUI.BeginChangeCheck();

        topicName = EditorGUILayout.TextField("Topic Name", topicName);
        gradeIndex = EditorGUILayout.Popup("Grade", gradeIndex, grades);
        subjectIndex = EditorGUILayout.Popup("Subject", subjectIndex, subjects);
        url = EditorGUILayout.TextField("Base URL", RemoveLastSlash(url));

        string RemoveLastSlash(string input)
        {
            if (input.EndsWith("/"))
                input = input.Substring(0, input.Length - 1);
            return input;
        }

        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetString(KEY_TOPIC, topicName);
            EditorPrefs.SetInt(KEY_GRADE, gradeIndex);
            EditorPrefs.SetInt(KEY_SUBJECT, subjectIndex);
            EditorPrefs.SetString(KEY_URL, url);
        }

        GUILayout.Space(10);

        // ====== PREVIEW ======
        string gradeShort = grades[gradeIndex];
        string gradeFull = gradeShort.Replace("G", "Grade");
        string subject = subjects[subjectIndex];
        string pascalTopic = gradeShort + ToPascalCase(topicName);

        // Find current folder names (if exist)
        string currentWorkspace = FindFolder("Assets", "SL_Workspace_");
        string currentVoiceOver = FindFolder("Assets/Resources_moved/VoiceOvers", null);
        string currentGradeScene = FindGradeScenePath();
        string currentScenes = FindAssessmentScenes();
        (string currentGroup, string currentKeys) = FindCurrentAddressableGroup();


        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

        if (string.IsNullOrEmpty(topicName) || string.IsNullOrEmpty(url))
        {
            // Skeleton placeholders
            EditorGUILayout.LabelField("Topic ID:", "G*TopicName");
            EditorGUILayout.LabelField("BuildPath:", "ServerData/G*/G*TopicName/[BuildTarget]");
            EditorGUILayout.LabelField("LoadPath:", "http://yourserver/G*/G*TopicName/[BuildTarget]");
            EditorGUILayout.LabelField("Workspace:", "SL_Workspace_OldName → SL_Workspace_TopicName");
            EditorGUILayout.LabelField("VoiceOvers:", "OldFolder → TopicName");
            EditorGUILayout.LabelField("GradeScene:", $"{currentGradeScene} → Grade*/Subject/TopicName");
            EditorGUILayout.LabelField("GradeScene:", $"{currentGradeScene} → Grade*/Subject/TopicName");
            EditorGUILayout.LabelField("Assessment Scenes:", $"{currentScenes} → TopicName_Assessment , TopicName_MainMenu");

            EditorGUILayout.LabelField("Addressable Group:", $"{currentGroup} → TopicName");
            EditorGUILayout.LabelField("Addressable Keys:", $"{currentKeys} → TopicName_MainMenu , TopicName_Assessment");
        }
        else
        {
            // Real preview
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Topic ID:", pascalTopic);
            if (GUILayout.Button("Copy", GUILayout.Width(70), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
                EditorGUIUtility.systemCopyBuffer = pascalTopic;
            EditorGUILayout.EndHorizontal();


            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var profileId = settings != null ? settings.activeProfileId : null;
            //EditorGUILayout.LabelField("BuildPath:", $"ServerData/{gradeShort}/{pascalTopic}/[BuildTarget]");
            //EditorGUILayout.LabelField("LoadPath:", $"{url}/{gradeShort}/{pascalTopic}/[BuildTarget]");
            DrawCompareLabel("BuildPath:", settings.profileSettings.GetValueByName(profileId, "Remote.BuildPath"), $"ServerData/{gradeShort}/{pascalTopic}/[BuildTarget]");
            DrawCompareLabel("LoadPath:", settings.profileSettings.GetValueByName(profileId, "Remote.LoadPath"), $"{url}/{gradeShort}/{pascalTopic}/[BuildTarget]");
            DrawCompareLabel("Workspace:", currentWorkspace, $"SL_Workspace_{pascalTopic}");
            DrawCompareLabel("VoiceOvers:", currentVoiceOver, pascalTopic);
            DrawCompareLabel("GradeScene:", currentGradeScene, $"{gradeFull}/{subject}/{pascalTopic}");
            DrawCompareLabel("Assessment Scenes:", currentScenes, $"{pascalTopic}_Assessment , {pascalTopic}_MainMenu");
            DrawCompareLabel("Addressable Group:", currentGroup, pascalTopic);
            DrawCompareLabel("Addressable Keys:", currentKeys, $"{pascalTopic}_Assessment, {pascalTopic}_MainMenu");
        }


        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // ====== BUTTONS IN ROWS ======
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fixedHeight = 28;
        buttonStyle.fontStyle = FontStyle.Bold;

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Apply Addressables", buttonStyle)) { ApplyAddressablesProfile(); }
        if (GUILayout.Button("Apply Product Name", buttonStyle)) { ApplyProductName(); }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Rename VoiceOvers", buttonStyle)) { RenameVoiceOversFolder(); }
        if (GUILayout.Button("Rename SL Workspace", buttonStyle)) { RenameWorkspaceFolders(); }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(5);

        if (GUILayout.Button("Rename GradeScene Folders", buttonStyle)) { RenameGradeSceneFolders(); }
        if (GUILayout.Button("Rename Assessment Scenes", buttonStyle))
        {
            RenameAssessmentScenes();
        }

        GUI.enabled = false;
        if (GUILayout.Button("Update Addressable Group", buttonStyle))
        {
            if (!string.IsNullOrEmpty(pascalTopic))
                UpdateAddressableGroup(pascalTopic);
        }
        GUI.enabled = true;

        GUILayout.Space(5);
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);
    }

    void DrawCompareLabel(string label, string currentValue, string targetValue)
    {
        bool matched = currentValue == targetValue;

        GUIContent icon = EditorGUIUtility.IconContent(
            matched ? "TestPassed" : "TestFailed"
        );

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(
            label,
            $"{currentValue} → {targetValue}"
        );

        GUILayout.Label(icon, GUILayout.Width(20));

        EditorGUILayout.EndHorizontal();
    }

    void ApplyAddressablesProfile()
    {
        if (string.IsNullOrEmpty(topicName) || string.IsNullOrEmpty(url))
        {
            EditorUtility.DisplayDialog("Error", "Please enter Topic Name and URL.", "OK");
            return;
        }

        string gradeShort = grades[gradeIndex];
        string pascalTopic = gradeShort + ToPascalCase(topicName);

        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings != null)
        {
            var profileSettings = settings.profileSettings;
            var profileId = settings.activeProfileId;

            profileSettings.SetValue(profileId, "Remote.BuildPath",
                $"ServerData/{gradeShort}/{pascalTopic}/[BuildTarget]");

            profileSettings.SetValue(profileId, "Remote.LoadPath",
                $"{url}/{gradeShort}/{pascalTopic}/[BuildTarget]");

            Debug.Log("Updated Addressables BuildPath and LoadPath for active profile.");
        }
        else
        {
            Debug.LogWarning("Addressable settings not found. Please initialize Addressables first.");
        }
    }

    void ApplyProductName()
    {
        if (string.IsNullOrEmpty(topicName))
        {
            EditorUtility.DisplayDialog("Error", "Please enter Topic Name.", "OK");
            return;
        }

        PlayerSettings.productName = topicName;
        Debug.Log("Updated Product Name to " + topicName);
    }

    void RenameVoiceOversFolder()
    {
        if (string.IsNullOrEmpty(topicName)) return;

        string gradeShort = grades[gradeIndex];
        string pascalTopic = gradeShort + ToPascalCase(topicName);
        string sourcePath = "Assets/Resources_moved/VoiceOvers";

        if (Directory.Exists(sourcePath))
        {
            string[] subDirs = Directory.GetDirectories(sourcePath);
            if (subDirs.Length > 0)
            {
                string oldFolder = subDirs[0];
                string newFolder = Path.Combine(sourcePath, pascalTopic);

                if (oldFolder != newFolder)
                {
                    AssetDatabase.MoveAsset(oldFolder.Replace("\\", "/"), newFolder.Replace("\\", "/"));
                    Debug.Log($"Renamed folder {oldFolder} → {newFolder}");
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    void RenameWorkspaceFolders()
    {
        if (string.IsNullOrEmpty(topicName)) return;

        string gradeShort = grades[gradeIndex];
        string pascalTopic = gradeShort + ToPascalCase(topicName);

        foreach (string workspace in Directory.GetDirectories("Assets", "SL_Workspace_*"))
        {
            string newName = $"SL_Workspace_{pascalTopic}";
            string oldAssetPath = workspace.Replace("\\", "/");
            string newAssetPath = Path.Combine("Assets", newName).Replace("\\", "/");

            if (oldAssetPath != newAssetPath)
            {
                AssetDatabase.MoveAsset(oldAssetPath, newAssetPath);
                Debug.Log($"Renamed workspace {oldAssetPath} → {newAssetPath}");
            }

            // Handle Resources and Resources_moved inside AutoGenerated
            RenameFirstSubfolder(Path.Combine(newAssetPath, "AutoGenerated/Resources"), pascalTopic);
            RenameFirstSubfolder(Path.Combine(newAssetPath, "AutoGenerated/Resources_moved"), pascalTopic);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    void RenameGradeSceneFolders()
    {
        if (string.IsNullOrEmpty(topicName)) return;

        string gradeShort = grades[gradeIndex];
        string pascalTopic = gradeShort + ToPascalCase(topicName);
        string gradeFull = grades[gradeIndex].Replace("G", "Grade");
        string subject = subjects[subjectIndex];

        string rootPath = "Assets/GradeScene(Addressables)";
        if (!Directory.Exists(rootPath))
        {
            Debug.LogWarning($"Root GradeScene path not found: {rootPath}");
            return;
        }

        // Rename grade folder
        string[] gradeDirs = Directory.GetDirectories(rootPath);
        if (gradeDirs.Length > 0)
        {
            string oldGrade = gradeDirs[0];
            string newGrade = Path.Combine(rootPath, gradeFull);
            AssetDatabase.MoveAsset(oldGrade.Replace("\\", "/"), newGrade.Replace("\\", "/"));

            // Rename subject folder
            string[] subjectDirs = Directory.GetDirectories(newGrade);
            if (subjectDirs.Length > 0)
            {
                string oldSubject = subjectDirs[0];
                string newSubject = Path.Combine(newGrade, subject);
                AssetDatabase.MoveAsset(oldSubject.Replace("\\", "/"), newSubject.Replace("\\", "/"));

                // Rename topic folder
                string[] topicDirs = Directory.GetDirectories(newSubject);
                if (topicDirs.Length > 0)
                {
                    string oldTopic = topicDirs[0];
                    string newTopic = Path.Combine(newSubject, pascalTopic);
                    AssetDatabase.MoveAsset(oldTopic.Replace("\\", "/"), newTopic.Replace("\\", "/"));
                    Debug.Log($"Renamed GradeScene topic {oldTopic} → {newTopic}");
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    void RenameFirstSubfolder(string parentPath, string newName)
    {
        if (Directory.Exists(parentPath))
        {
            string[] subDirs = Directory.GetDirectories(parentPath);
            foreach (string oldFolder in subDirs)
            {
                string newFolder = Path.Combine(parentPath, newName);
                AssetDatabase.MoveAsset(oldFolder.Replace("\\", "/"), newFolder.Replace("\\", "/"));
                Debug.Log($"Renamed {oldFolder} → {newFolder}");
            }
        }
    }

    void RenameAssessmentScenes()
    {
        if (string.IsNullOrEmpty(topicName)) return;

        string gradeShort = grades[gradeIndex];
        string pascalTopic = gradeShort + ToPascalCase(topicName);
        string scenePath = "Assets/Assessment/Scenes";

        if (!Directory.Exists(scenePath))
        {
            Debug.LogWarning($"Scene path not found: {scenePath}");
            return;
        }

        // Look for existing scenes and rename
        foreach (string file in Directory.GetFiles(scenePath, "*.unity"))
        {
            string oldFile = file.Replace("\\", "/");
            string fileName = Path.GetFileNameWithoutExtension(oldFile);

            if (fileName.EndsWith("_Assessment"))
            {
                string newFile = Path.Combine(scenePath, $"{pascalTopic}_Assessment.unity").Replace("\\", "/");
                if (oldFile != newFile)
                {
                    AssetDatabase.MoveAsset(oldFile, newFile);
                    Debug.Log($"Renamed scene {oldFile} → {newFile}");
                }
            }
            else if (fileName.EndsWith("_MainMenu"))
            {
                string newFile = Path.Combine(scenePath, $"{pascalTopic}_MainMenu.unity").Replace("\\", "/");
                if (oldFile != newFile)
                {
                    AssetDatabase.MoveAsset(oldFile, newFile);
                    Debug.Log($"Renamed scene {oldFile} → {newFile}");
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    void UpdateAddressableGroup(string topicPascal)
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("Addressable settings not found.");
            return;
        }

        // Ensure group exists (rename if already exists with old topic name)
        var group = settings.FindGroup(topicPascal);
        if (group == null)
        {
            group = settings.CreateGroup(topicPascal, false, false, false, null, typeof(BundledAssetGroupSchema));
            Debug.Log($"Created group: {topicPascal}");
        }

        string scenesPath = "Assets/Assessment/Scenes";
        if (!AssetDatabase.IsValidFolder(scenesPath))
        {
            Debug.LogWarning($"Scenes folder not found: {scenesPath}");
            return;
        }

        foreach (string file in Directory.GetFiles(scenesPath, "*.unity"))
        {
            string assetPath = file.Replace("\\", "/");
            string fileName = Path.GetFileNameWithoutExtension(assetPath);

            if (fileName.EndsWith("_Assessment") || fileName.EndsWith("_MainMenu"))
            {
                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                if (string.IsNullOrEmpty(guid)) continue;

                var entry = settings.FindAssetEntry(guid);
                if (entry == null)
                {
                    entry = settings.CreateOrMoveEntry(guid, group);
                    Debug.Log($"Added {fileName} to group {topicPascal}");
                }
                else
                {
                    entry.parentGroup = group;
                    Debug.Log($"Moved {fileName} to group {topicPascal}");
                }

                // Set key = file name
                entry.SetAddress(fileName);
            }
        }

        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }



    string ToPascalCase(string input)
    {
        TextInfo ti = CultureInfo.InvariantCulture.TextInfo;
        string[] words = input.Split(new char[] { ' ', '_', '-' }, System.StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < words.Length; i++)
            words[i] = ti.ToTitleCase(words[i].ToLower());
        return string.Join("", words);
    }

    string FindFolder(string parentPath, string prefix)
    {
        if (!AssetDatabase.IsValidFolder(parentPath)) return "**";

        foreach (var sub in AssetDatabase.GetSubFolders(parentPath))
        {
            string folderName = System.IO.Path.GetFileName(sub);
            if (string.IsNullOrEmpty(prefix) || folderName.StartsWith(prefix))
                return folderName;
        }
        return "**";
    }

    string FindGradeScenePath()
    {
        string root = "Assets/GradeScene(Addressables)";
        if (!AssetDatabase.IsValidFolder(root)) return "**";

        // Step 1: grade
        var gradeFolders = AssetDatabase.GetSubFolders(root);
        if (gradeFolders.Length == 0) return "**";
        string gradeFolder = System.IO.Path.GetFileName(gradeFolders[0]);

        // Step 2: subject
        var subjectFolders = AssetDatabase.GetSubFolders(gradeFolders[0]);
        if (subjectFolders.Length == 0) return gradeFolder;
        string subjectFolder = System.IO.Path.GetFileName(subjectFolders[0]);

        // Step 3: topic
        var topicFolders = AssetDatabase.GetSubFolders(subjectFolders[0]);
        if (topicFolders.Length == 0) return $"{gradeFolder}/{subjectFolder}";
        string topicFolder = System.IO.Path.GetFileName(topicFolders[0]);

        return $"{gradeFolder}/{subjectFolder}/{topicFolder}";
    }

    string FindAssessmentScenes()
    {
        string path = "Assets/Assessment/Scenes";
        if (!AssetDatabase.IsValidFolder(path)) return "**";

        string currentAssessment = null;
        string currentMainMenu = null;

        foreach (string file in Directory.GetFiles(path, "*.unity"))
        {
            string name = Path.GetFileNameWithoutExtension(file);
            if (name.EndsWith("_Assessment")) currentAssessment = name;
            else if (name.EndsWith("_MainMenu")) currentMainMenu = name;
        }

        if (string.IsNullOrEmpty(currentAssessment) && string.IsNullOrEmpty(currentMainMenu))
            return "**";

        return $"{currentAssessment ?? "??"} , {currentMainMenu ?? "??"}";
    }

    (string currentGroup, string currentKeys) FindCurrentAddressableGroup()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null) return ("**", "**");

        // Just return the first group that has MainMenu or Assessment scenes
        foreach (var group in settings.groups)
        {
            if (group == null) continue;

            var keys = new List<string>();
            foreach (var entry in group.entries)
            {
                if (entry.address.EndsWith("_Assessment") || entry.address.EndsWith("_MainMenu"))
                    keys.Add(entry.address);
            }

            if (keys.Count > 0)
                return (group.Name, string.Join(", ", keys));
        }

        return ("**", "**");
    }

}

