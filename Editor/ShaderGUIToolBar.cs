
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
namespace NBShaderEditor
{

    public class ShaderGUIToolBar
    {
        public ShaderGUIHelper Helper;

        private int viewModeIndex;
        private readonly string[] viewModes = { "List", "Grid" };
        private string searchText = "";
        private MaterialEditor _editor=>Helper.matEditor;
        public ShaderGUIToolBar(ShaderGUIHelper helper)
        {
            Helper = helper;
        }

        private static Material copiedMaterial;
        private static Shader copiedShader;
        
        // 帮助链接URL
        private const string HELP_URL = "https://owejt9diz2c.feishu.cn/wiki/BHz8wHHSjiYJagk7WrmcAcconlb?from=from_copylink";

        public void DrawToolbar()
        {

            float BtnWidth = 30f;
            // 开始工具栏区域 (背景)
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        
            // 1. 选择当前材质
            if (GUILayout.Button(EditorGUIUtility.IconContent("Material On Icon","跳到当前材质|跳到当前材质"), EditorStyles.toolbarButton,GUILayout.Width(BtnWidth)))
            {
                EditorGUIUtility.PingObject(Helper.mats[0]);
            }
            
            if (GUILayout.Button(EditorGUIUtility.IconContent("TreeEditor.Trash","清除没有使用的贴图|清除没有使用的贴图"), EditorStyles.toolbarButton,GUILayout.Width(BtnWidth)))
            {
                foreach (var mat in Helper.mats)
                {
                    CleanUnusedTextureProperties(Helper.mats[0]);//先清理不属于当前Shader的贴图
                }
                Helper.isClearUnUsedTexture = true;
            }
            
            if (GUILayout.Button(new GUIContent("C","复制材质属性"), EditorStyles.toolbarButton,GUILayout.Width(BtnWidth)))
            {
                copiedMaterial = Helper.mats[0];
                copiedShader = copiedMaterial.shader;
            }
            
            if (GUILayout.Button(new GUIContent("V","粘贴材质属性"), EditorStyles.toolbarButton,GUILayout.Width(BtnWidth)))
            {
                if (copiedShader)
                {
                    Helper.mats[0].shader = copiedShader;
                }

                if (copiedMaterial)
                {
                    Helper.mats[0].CopyPropertiesFromMaterial(copiedMaterial);
                }
                
            }
            

            // 2. 添加下拉菜单
            // viewModeIndex = EditorGUILayout.Popup(viewModeIndex, viewModes, EditorStyles.toolbarPopup, GUILayout.Width(80));

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

            // // 4. 右侧按钮组
            // if (GUILayout.Button(EditorGUIUtility.IconContent("TreeEditor.Refresh"), EditorStyles.toolbarButton))
            // {
            //     Debug.Log("Refresh clicked");
            // }
            //
            // // 选项菜单
            // if (GUILayout.Button(EditorGUIUtility.IconContent("_Popup"), EditorStyles.toolbarButton))
            // {
            //     // 创建下拉菜单
            //     GenericMenu menu = new GenericMenu();
            //     menu.AddItem(new GUIContent("Option 1"), false, () => Debug.Log("Option 1"));
            //     menu.AddItem(new GUIContent("Option 2"), false, () => Debug.Log("Option 2"));
            //     menu.ShowAsContext();
            // }
            
            if (GUILayout.Button(EditorGUIUtility.IconContent("d__Help@2x","说明文档|说明文档"), EditorStyles.toolbarButton,GUILayout.Width(BtnWidth)))
            {
                // 打开浏览器跳转到帮助链接
                Application.OpenURL(HELP_URL);
            }

            EditorGUILayout.EndHorizontal(); // 结束工具栏
        }
        
        private void CleanUnusedTextureProperties(Material mat)
        {
            if (mat == null || mat.shader == null) return;

            Shader shader = mat.shader;

            // 收集 Shader 里声明过的贴图属性
            var shaderTexProps = new HashSet<string>();
            int count = ShaderUtil.GetPropertyCount(shader);
            for (int i = 0; i < count; i++)
            {
                if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                {
                    shaderTexProps.Add(ShaderUtil.GetPropertyName(shader, i));
                }
            }

            // 遍历材质所有贴图属性，找到 shader 不再声明的
            var allProps = mat.GetTexturePropertyNames();
            foreach (var propName in allProps)
            {
                if (!shaderTexProps.Contains(propName))
                {
                    if (mat.GetTexture(propName) != null)
                    {
                        mat.SetTexture(propName, null);
                        Debug.Log($"清理 {mat.name} 的无效贴图属性: {propName}");
                    }
                }
            }
        }

    }
}