using System.Collections.Generic;
using System;
using PlasticGui;
using UnityEngine;
using UnityEngine.UIElements;
using ShaderPropertyPack = UnityEditor.ShaderGUIHelper.ShaderPropertyPack;

namespace UnityEditor
{
    public class ShaderGUIResetTool
    {
        private ShaderGUIHelper _helper;
        private Shader _shader;
        private bool _isInitResetData = false;
        
        private Stack<(string,string)> _scopeContextStack = new Stack<(string,string)>();

        public void CheckAllModifyOnValueChange()
        {
            foreach (var item in ResetItemDict.Values)
            {
                item.HasModified = item.CheckHasModifyOnValueChange();
            }
        }
        public void Init(ShaderGUIHelper helper)
        {
            _helper = helper;
            _shader = helper.shader;
            _isInitResetData = true;
            ResetItemDict.Clear();
            _scopeContextStack.Clear();
        }

        public void EndInit()
        {
            _isInitResetData = false;
        }

        public void Update()
        {
            if (_needUpdate)
            {
                _needUpdate = false;
                CheckAllModifyOnValueChange();
            }
        }

        private bool _needUpdate = false;
        public void NeedUpdate()
        {
            _needUpdate = true;
        }

        public ShaderGUIResetTool(ShaderGUIHelper helper)
        {
            Init(helper);
        }

        public Dictionary<(string, string), ResetItem> ResetItemDict = new Dictionary<(string, string), ResetItem>();

        public class ResetItem
        {
            public ResetItem Parent;
            public List<ResetItem> ChildResetItems = new List<ResetItem>();
            public Action ResetCallBack;
            public Action OnValueChangedCallBack;
            public Func<bool> CheckHasModifyOnValueChange;
            public (string, string) NameTuple;
            public bool HasModified = false;

            public ResetItem((string, string) nameTuple,Action resetCallBack,Action onValueChangedCallBack,Func<bool> checkHasModifyOnValueChange)
            {
                NameTuple = nameTuple;
                ResetCallBack = resetCallBack;
                OnValueChangedCallBack = onValueChangedCallBack;
                CheckHasModifyOnValueChange = checkHasModifyOnValueChange;
            }

            public void Execute()
            {
                ResetCallBack?.Invoke();
                OnValueChangedCallBack?.Invoke();
                foreach (var childItem in ChildResetItems)
                {
                    childItem.Execute();
                }
            }
        }

        public void CheckHasModifyOnValueChange((string,string) nameTuple)
        {
            var resetItem = ResetItemDict[nameTuple];
            if (resetItem != null)
            {
               resetItem.HasModified = resetItem.CheckHasModifyOnValueChange();
            }
        }
        public void DrawResetModifyButton(string label,ShaderPropertyPack pack, Action resetAction = null,Action onValueChangedCallBack = null)
        {
            (string, string) nameTuple = (label, pack.property.name);
            ConstructResetItem(nameTuple,
                resetAction: ()=>{
                    SetPropertyToDefaultValue(pack); 
                    resetAction?.Invoke();
                },onValueChangedCallBack:onValueChangedCallBack,
                checkHasModifyOnValueChange: () => IsPropertyModified(pack)
                );
            ResetItem item = ResetItemDict[nameTuple];
            DrawResetModifyButton(item.HasModified, pack.property.hasMixedValue, () =>
            {
                item.Execute();
            });
        }

        public void DrawResetModifyButton(string label)
        {
            ConstructResetItem((label, ""));
            ResetItem thisItem = ResetItemDict[(label, "")];
            if (_isInitResetData)
            {
                thisItem.CheckHasModifyOnValueChange = () =>
                {
                    foreach (var item in thisItem.ChildResetItems)
                    {
                        if (item.HasModified) return true;
                    }
                    return false;
                };
            }

            thisItem.HasModified = thisItem.CheckHasModifyOnValueChange();
            DrawResetModifyButton(thisItem.HasModified,false,thisItem.Execute);
        }

        public void ConstructResetItem((string,string) nameTuple, Action resetAction = null,
            Action onValueChangedCallBack = null,Func<bool> checkHasModifyOnValueChange = null)
        {
            if(!_isInitResetData) return;
            ResetItem item = new ResetItem(nameTuple,resetAction,onValueChangedCallBack,checkHasModifyOnValueChange);
            if (checkHasModifyOnValueChange != null)
            {
                item.HasModified = checkHasModifyOnValueChange.Invoke();//初始化参数
            }
            ResetItemDict.Add(nameTuple,item);
            if (_scopeContextStack.Count > 0)
            {
                var contextNameTuple = _scopeContextStack.Peek();
                ResetItem parentItem = ResetItemDict[contextNameTuple];
                parentItem.ChildResetItems.Add(item);
                item.Parent = parentItem;
            }
            _scopeContextStack.Push(nameTuple);
        }
        
        
        public void EndResetModifyButtonScope()
        {
            if(!_isInitResetData) return;
            if(_scopeContextStack.Count == 0) return;
            _scopeContextStack.Pop();
        }
        

        //仅仅只是Drawer
        private void DrawResetModifyButton(bool hasModified, bool hasMixedValue,Action onResetButton)
        {
            // GUILayout.FlexibleSpace();

            float btnSize = EditorGUIUtility.singleLineHeight;
            if (hasModified || hasMixedValue)
            {
                if (GUILayout.Button("R", GUILayout.Width(btnSize), GUILayout.Height(btnSize)))
                {
                    onResetButton?.Invoke();
                }
            }
            else
            {
                GUILayout.Label("",GUILayout.Width(btnSize), GUILayout.Height(btnSize));
            }

        }


     
        public void SetPropertyToDefaultValue(ShaderPropertyPack pack)
        {
            MaterialProperty property = pack.property;
            switch (pack.property.type)
            {
                case MaterialProperty.PropType.Color:
                    Vector4 vecValue = _shader.GetPropertyDefaultVectorValue(pack.index);
                    property.colorValue = new Color(vecValue.x, vecValue.y, vecValue.z, vecValue.x);
                    break;

                case MaterialProperty.PropType.Vector:
                    property.vectorValue = _shader.GetPropertyDefaultVectorValue(pack.index);
                    break;

                case MaterialProperty.PropType.Float or MaterialProperty.PropType.Range:
                    float value = _shader.GetPropertyDefaultFloatValue(pack.index);
                    property.floatValue = value;
                    break;

                case MaterialProperty.PropType.Texture:
                    if (property.textureValue == null)
                    {
                        break;
                    }
                    else
                    {
                        //TODO 如何ResetTexture；
                        break;
                    }
                // return property.textureValue.name == shader.GetPropertyTextureDefaultName(pack.index) ? false : true;

                default:
                    // 如果不属于上述类型，输出提示信息
                    Debug.Log($"{property.displayName} has no default value or unsupported type");
                    break;
            }
        }

        public bool IsPropertyModified(ShaderPropertyPack pack)
        {
            MaterialProperty property = pack.property;
            switch (pack.property.type)
            {
                case MaterialProperty.PropType.Color:
                    Vector4 vecValue = _shader.GetPropertyDefaultVectorValue(pack.index);
                    Color color = new Color(vecValue.x, vecValue.y, vecValue.z, vecValue.w);
                    return property.colorValue == color ? false : true;

                case MaterialProperty.PropType.Vector:
                    return property.vectorValue == _shader.GetPropertyDefaultVectorValue(pack.index) ? false : true;

                case MaterialProperty.PropType.Float or MaterialProperty.PropType.Range:
                    return Mathf.Approximately(property.floatValue, _shader.GetPropertyDefaultFloatValue(pack.index)) ? false : true;

                case MaterialProperty.PropType.Texture:
                    if (property.textureValue == null)
                    {
                        return true;
                    }
                    else
                    {
                        return property.textureValue.name == "textureExternal" ? false : true;
                    }
                // return property.textureValue.name == shader.GetPropertyTextureDefaultName(pack.index) ? false : true;

                default:
                    // 如果不属于上述类型，输出提示信息
                    return false;
                    Debug.Log($"{property.displayName} has no default value or unsupported type");
                    break;
            }
        }
    }

}