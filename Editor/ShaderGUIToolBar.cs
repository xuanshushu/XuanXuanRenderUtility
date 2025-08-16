
using UnityEngine;
using UnityEditor;
namespace NBShaderEditor
{

    public class ShaderGUIToolBar
    {
        public ShaderGUIHelper Helper;

        private int viewModeIndex;
        private readonly string[] viewModes = { "List", "Grid" };
        private string searchText = "";
        public ShaderGUIToolBar(ShaderGUIHelper helper)
        {
            Helper = helper;
        }

        public void DrawToolbar()
        {
            // 开始工具栏区域 (背景)
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        
            // 1. 添加按钮
            if (GUILayout.Button(EditorGUIUtility.IconContent("CreateAddNew"), EditorStyles.toolbarButton))
            {
                Debug.Log("Create button clicked");
                // 这里添加创建菜单逻辑
            }

            // 2. 添加下拉菜单
            viewModeIndex = EditorGUILayout.Popup(viewModeIndex, viewModes, EditorStyles.toolbarPopup, GUILayout.Width(80));

            // 3. 添加搜索框
            GUILayout.FlexibleSpace(); // 将搜索框推到中间
        
            // 搜索框样式
            GUIStyle searchField = new GUIStyle("SearchTextField");
            GUIStyle cancelButton = new GUIStyle("SearchCancelButton");
        
            EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(300));
            {
                searchText = EditorGUILayout.TextField(searchText, searchField);
            
                // 清除搜索按钮
                if (GUILayout.Button("", cancelButton))
                {
                    searchText = "";
                    GUI.FocusControl(null); // 移除焦点
                }
            }
            EditorGUILayout.EndHorizontal();

            // 4. 右侧按钮组
            if (GUILayout.Button(EditorGUIUtility.IconContent("TreeEditor.Refresh"), EditorStyles.toolbarButton))
            {
                Debug.Log("Refresh clicked");
            }

            // 选项菜单
            if (GUILayout.Button(EditorGUIUtility.IconContent("_Popup"), EditorStyles.toolbarButton))
            {
                // 创建下拉菜单
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Option 1"), false, () => Debug.Log("Option 1"));
                menu.AddItem(new GUIContent("Option 2"), false, () => Debug.Log("Option 2"));
                menu.ShowAsContext();
            }

            EditorGUILayout.EndHorizontal(); // 结束工具栏
        }

    }
}