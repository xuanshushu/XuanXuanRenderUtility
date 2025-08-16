using System.Collections.Generic;
using System;
using PlasticGui;
using UnityEngine;
using UnityEngine.UIElements;
using ShaderPropertyPack = NBShaderEditor.ShaderGUIHelper.ShaderPropertyPack;
using UnityEditor;
namespace NBShaderEditor
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

        public void DrawResetModifyButton(string label, ShaderPropertyPack pack, Action resetAction = null,
            Action onValueChangedCallBack = null)
        {
            DrawResetModifyButton(new Rect(),label,pack,resetAction,onValueChangedCallBack);
        }

        public void DrawResetModifyButton(Rect rect, string label, ShaderPropertyPack pack, Action resetAction = null,
            Action onValueChangedCallBack = null, VectorValeType vectorValeType = VectorValeType.Undefine)
        {
            DrawResetModifyButton(rect, (label,pack.property.name), pack, resetAction, onValueChangedCallBack, vectorValeType);
        }
        public void DrawResetModifyButton(Rect rect,(string,string)nameTuple,ShaderPropertyPack pack, Action resetAction = null,Action onValueChangedCallBack = null,VectorValeType vectorValeType = VectorValeType.Undefine)
        {
            
            // (string, string) nameTuple = (label, pack.property.name);
            ConstructResetItem(nameTuple,
                resetAction: ()=>{
                    SetPropertyToDefaultValue(pack,vectorValeType); 
                    resetAction?.Invoke();
                },onValueChangedCallBack:()=>
                {
                    onValueChangedCallBack?.Invoke();//这个里面必须要写CheckHasModifyOnValueChange。因为不在Reset的时候，属性值本身变化的时候也要看看修改了没。
                },
                checkHasModifyOnValueChange: () => IsPropertyModified(pack,vectorValeType)
                );
            if (ResetItemDict.ContainsKey(nameTuple))
            {
                ResetItem item = ResetItemDict[nameTuple];
                DrawResetModifyButton(rect,item.HasModified, pack.property.hasMixedValue, () =>
                {
                    item.Execute();
                });
            }
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
            DrawResetModifyButton(new Rect(),thisItem.HasModified,false,thisItem.Execute);
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

        public float ResetButtonSize => EditorGUIUtility.singleLineHeight;

        private GUIContent resetIconContent = new GUIContent();
        //仅仅只是Drawer
        private void DrawResetModifyButton(Rect position,bool hasModified, bool hasMixedValue,Action onResetButton)
        {
            // GUILayout.FlexibleSpace();

            float btnSize = ResetButtonSize;
            string iconText;
            bool isDisabled = true;
            GUIStyle iconStyle;
            if (hasModified || hasMixedValue)
            {
                isDisabled = false;
                iconText = "R";
                iconStyle = GUI.skin.button;
            }
            else
            {
                isDisabled = true;
                iconText = "";
                iconStyle = GUI.skin.label;
            }

            resetIconContent.text = iconText;
            EditorGUI.BeginDisabledGroup(isDisabled);
            // if (GUILayout.Button(iconTexture, GUILayout.Width(btnSize), GUILayout.Height(btnSize)))
            if (position.width <= 0)
            {
                position = GUILayoutUtility.GetRect(resetIconContent, iconStyle, GUILayout.Width(btnSize),
                    GUILayout.Height(btnSize));
            }
            if(GUI.Button(position,resetIconContent,iconStyle))
            {
                
                onResetButton?.Invoke();
            }
            EditorGUI.EndDisabledGroup();
        }
     
        public void SetPropertyToDefaultValue(ShaderPropertyPack pack,VectorValeType vectorValeType = VectorValeType.Undefine)
        {
            MaterialProperty property = pack.property;
            MaterialProperty.PropType propertyType = property.type;
            if (pack.property.type == MaterialProperty.PropType.Texture && vectorValeType != VectorValeType.Undefine)
            {
                propertyType = MaterialProperty.PropType.Vector;//Tilling or Offset
            }
            switch (propertyType)
            {
                case MaterialProperty.PropType.Color:
                    Vector4 colorValue = _shader.GetPropertyDefaultVectorValue(pack.index);
                    property.colorValue = new Color(colorValue.x, colorValue.y, colorValue.z, colorValue.x);
                    break;

                case MaterialProperty.PropType.Vector:
                    Vector4 defaultVecValue;
                    Vector4 vecValue;
                    if (vectorValeType == VectorValeType.Tilling || vectorValeType == VectorValeType.Offset)
                    {
                        defaultVecValue = new Vector4(1f, 1f, 0f, 0f);
                        vecValue = property.textureScaleAndOffset;
                        
                    }
                    else
                    {
                        defaultVecValue = _shader.GetPropertyDefaultVectorValue(pack.index);
                        vecValue = property.vectorValue;
                    }
                    switch (vectorValeType)
                    {
                        case VectorValeType.Undefine: Debug.LogError("VectorValeType is undefined"); break;
                        case VectorValeType.X: vecValue.x = defaultVecValue.x;property.vectorValue = vecValue;break;
                        case VectorValeType.Y: vecValue.y = defaultVecValue.y;property.vectorValue = vecValue;break;
                        case VectorValeType.Z: vecValue.z = defaultVecValue.z;property.vectorValue = vecValue;break;
                        case VectorValeType.W: vecValue.w = defaultVecValue.w;property.vectorValue = vecValue;break;
                        case VectorValeType.XY:vecValue.x = defaultVecValue.x; vecValue.y = defaultVecValue.y;
                            property.vectorValue = vecValue;break;
                        case VectorValeType.Tilling:vecValue.x = defaultVecValue.x; vecValue.y = defaultVecValue.y;
                            property.textureScaleAndOffset = vecValue;break;
                        case VectorValeType.ZW:vecValue.z = defaultVecValue.z; vecValue.w = defaultVecValue.w;
                            property.vectorValue = vecValue;break;
                        case VectorValeType.Offset:vecValue.z = defaultVecValue.z; vecValue.w = defaultVecValue.w;
                            property.textureScaleAndOffset = vecValue;break;
                        case VectorValeType.XYZ:vecValue.x = defaultVecValue.x; vecValue.y = defaultVecValue.y;
                            vecValue.z = defaultVecValue.z; property.vectorValue = vecValue;break;
                        case VectorValeType.XYZW: property.vectorValue = defaultVecValue;break;
                    }
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
                        property.textureValue = null;
                        break;
                    }
                // return property.textureValue.name == shader.GetPropertyTextureDefaultName(pack.index) ? false : true;

                default:
                    // 如果不属于上述类型，输出提示信息
                    Debug.Log($"{property.displayName} has no default value or unsupported type");
                    break;
            }
        }

        public bool IsPropertyModified(ShaderPropertyPack pack,VectorValeType vectorValeType = VectorValeType.Undefine)
        {
            MaterialProperty property = pack.property;
            MaterialProperty.PropType propertyType = property.type;
            if (pack.property.type == MaterialProperty.PropType.Texture && vectorValeType != VectorValeType.Undefine)
            {
                propertyType = MaterialProperty.PropType.Vector;//Tilling or Offset
            }
            switch (propertyType)
            {
                case MaterialProperty.PropType.Color:
                    Vector4 colorValue = _shader.GetPropertyDefaultVectorValue(pack.index);
                    Color color = new Color(colorValue.x, colorValue.y, colorValue.z, colorValue.w);
                    return property.colorValue == color ? false : true;

                case MaterialProperty.PropType.Vector:

                    Vector4 defaultVecValue;
                    Vector4 vecValue;
                    if (vectorValeType == VectorValeType.Tilling || vectorValeType == VectorValeType.Offset)
                    {
                        defaultVecValue = new Vector4(1f, 1f, 0f, 0f);
                        vecValue = property.textureScaleAndOffset;
                        
                    }
                    else
                    {
                        defaultVecValue = _shader.GetPropertyDefaultVectorValue(pack.index);
                        vecValue = property.vectorValue;
                    }

                
                    Vector2 defaultVecXYValue = new Vector2(defaultVecValue.x, defaultVecValue.y);
                    Vector2 defaultVecZWValue = new Vector2(defaultVecValue.z, defaultVecValue.w);
                    Vector2 vecXYValue = new Vector2(vecValue.x, vecValue.y);
                    Vector2 vecZWValue = new Vector2(vecValue.z, vecValue.w);
                    Vector2 defaultVecXYZValue = new Vector3(defaultVecValue.x, defaultVecValue.y,defaultVecValue.z);
                    Vector2 vecXYZValue = new Vector3(vecValue.x, vecValue.y,vecValue.z);
                    

                    bool isVecModified = false;
                    switch (vectorValeType)
                    {
                        case VectorValeType.Undefine: Debug.LogError("VectorValeType is undefined"); break;
                        case VectorValeType.X: isVecModified = Mathf.Approximately(vecValue.x,defaultVecValue.x) ? false : true;break;
                        case VectorValeType.Y: isVecModified = Mathf.Approximately(vecValue.y,defaultVecValue.y) ? false : true;break;
                        case VectorValeType.Z: isVecModified = Mathf.Approximately(vecValue.z,defaultVecValue.z) ? false : true;break;
                        case VectorValeType.W: isVecModified = Mathf.Approximately(vecValue.w,defaultVecValue.w) ? false : true;break;
                        case VectorValeType.XY:case VectorValeType.Tilling:
                            isVecModified = vecXYValue == defaultVecXYValue ? false : true;break;
                        case VectorValeType.ZW:case VectorValeType.Offset:
                            isVecModified = vecZWValue == defaultVecZWValue ? false : true;break;
                        case VectorValeType.XYZ:isVecModified = vecXYZValue == defaultVecXYZValue ? false : true ; break;
                        case VectorValeType.XYZW:isVecModified=  vecValue == defaultVecValue? false : true ; break;
                    }
                    return isVecModified;
                    break;

                case MaterialProperty.PropType.Float or MaterialProperty.PropType.Range:
                    return Mathf.Approximately(property.floatValue, _shader.GetPropertyDefaultFloatValue(pack.index)) ? false : true;

                case MaterialProperty.PropType.Texture:
                    if (property.textureValue == null)
                    {
                        return false;
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