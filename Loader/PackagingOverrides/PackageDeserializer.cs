using ColossalFramework;
using ColossalFramework.Importers;
using ColossalFramework.Packaging;
using ColossalFramework.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Loader.PackagingOverrides {
    public static class PackageDeserializer {
        internal static bool IsUnityType(Type type) => type.IsEnum ||
                                                       type == typeof(bool) ||
                                                       type == typeof(byte) ||
                                                       type == typeof(int) ||
                                                       type == typeof(uint) ||
                                                       type == typeof(ulong) ||
                                                       type == typeof(float) ||
                                                       type == typeof(string) ||
                                                       type == typeof(bool[]) ||
                                                       type == typeof(byte[]) ||
                                                       type == typeof(int[]) ||
                                                       type == typeof(float[]) ||
                                                       type == typeof(string[]) ||
                                                       type == typeof(DateTime) ||
                                                       type == typeof(Package.Asset) ||
                                                       type == typeof(GameObject) ||
                                                       type == typeof(Vector2) ||
                                                       type == typeof(Vector3) ||
                                                       type == typeof(Vector4) ||
                                                       type == typeof(Color) ||
                                                       type == typeof(Matrix4x4) ||
                                                       type == typeof(Quaternion) ||
                                                       type == typeof(Vector2[]) ||
                                                       type == typeof(Vector3[]) ||
                                                       type == typeof(Vector4[]) ||
                                                       type == typeof(Color[]) ||
                                                       type == typeof(Matrix4x4[]) ||
                                                       type == typeof(Quaternion[]);

        private static string StripName(string name) {
            int num = name.LastIndexOf(".");
            return (num >= 0) ? name.Remove(0, num + 1) : name;
        }

        private static object CustomDeserialize(Package p, Type t, PackageReader r) {
            if (t == typeof(TransportInfo)) {
                return PrefabCollection<TransportInfo>.FindLoaded(r.ReadString());
            }
            if (t == typeof(ItemClass)) {
                return ItemClassCollection.FindClass(r.ReadString());
            }
            if (t == typeof(BuildingInfo.Prop)) {
                return new BuildingInfo.Prop {
                    m_prop = PrefabCollection<PropInfo>.FindLoaded(r.ReadString()),
                    m_tree = PrefabCollection<TreeInfo>.FindLoaded(r.ReadString()),
                    m_position = r.ReadVector3(),
                    m_angle = r.ReadSingle(),
                    m_probability = r.ReadInt32(),
                    m_fixedHeight = r.ReadBoolean()
                };
            }
            if (t == typeof(PropInfo.Variation)) {
                return new PropInfo.Variation {
                    m_prop = PrefabCollection<PropInfo>.FindLoaded(p.packageName + "." + r.ReadString()),
                    m_probability = r.ReadInt32()
                };
            }
            if (t == typeof(TreeInfo.Variation)) {
                return new TreeInfo.Variation {
                    m_tree = PrefabCollection<TreeInfo>.FindLoaded(p.packageName + "." + r.ReadString()),
                    m_probability = r.ReadInt32()
                };
            }
            if (t == typeof(BuildingInfo.PathInfo)) {
                BuildingInfo.PathInfo pathInfo = new BuildingInfo.PathInfo {
                    m_netInfo = PrefabCollection<NetInfo>.FindLoaded(r.ReadString()),
                    m_nodes = r.ReadVector3Array(),
                    m_curveTargets = r.ReadVector3Array(),
                    m_invertSegments = r.ReadBoolean(),
                    m_maxSnapDistance = r.ReadSingle()
                };
                if (p.version >= 5) {
                    pathInfo.m_forbidLaneConnection = r.ReadBooleanArray();
                    pathInfo.m_trafficLights = r.ReadBuilidngLights();
                    pathInfo.m_yieldSigns = r.ReadBooleanArray();
                }
                return pathInfo;
            }
            if (t == typeof(MessageInfo)) {
                MessageInfo messageInfo = new MessageInfo {
                    m_firstID1 = r.ReadString()
                };
                if (messageInfo.m_firstID1.Equals(string.Empty)) {
                    messageInfo.m_firstID1 = null;
                }
                messageInfo.m_firstID2 = r.ReadString();
                if (messageInfo.m_firstID2.Equals(string.Empty)) {
                    messageInfo.m_firstID2 = null;
                }
                messageInfo.m_repeatID1 = r.ReadString();
                if (messageInfo.m_repeatID1.Equals(string.Empty)) {
                    messageInfo.m_repeatID1 = null;
                }
                messageInfo.m_repeatID2 = r.ReadString();
                if (messageInfo.m_repeatID2.Equals(string.Empty)) {
                    messageInfo.m_repeatID2 = null;
                }
                return messageInfo;
            }
            if (t == typeof(AudioInfo)) {
                return null;
            }
            if (t == typeof(ModInfo)) {
                return new ModInfo {
                    modName = r.ReadString(),
                    modWorkshopID = r.ReadUInt64()
                };
            }
            if (t == typeof(DisasterProperties.DisasterSettings)) {
                return new DisasterProperties.DisasterSettings {
                    m_disasterName = r.ReadString(),
                    m_randomProbability = r.ReadInt32()
                };
            }
            if (t == typeof(UITextureAtlas)) {
                UITextureAtlas uITextureAtlas = ScriptableObject.CreateInstance<UITextureAtlas>();
                uITextureAtlas.name = r.ReadString();
                int num;
                int num2;
                Texture2D texture2D;
                if (p.version <= 3) {
                    num = r.ReadInt32();
                    num2 = r.ReadInt32();
                    texture2D = new Texture2D(num, num2, TextureFormat.ARGB32, false, false);
                    Color[] pixels = r.ReadColorArray();
                    texture2D.SetPixels(pixels);
                    texture2D.Apply();
                } else {
                    texture2D = new Texture2D(1, 1, TextureFormat.ARGB32, false, false);
                    texture2D.LoadImage(r.ReadByteArray());
                    num = texture2D.width;
                    num2 = texture2D.height;
                }
                string text = r.ReadString();
                Shader shader = Shader.Find(text);
                Material material = null;
                if (!(shader is null)) {
                    material = new Material(shader) {
                        mainTexture = texture2D
                    };
                } else {
                    Debug.Log("Warning: texture atlas shader *" + text + "* not found.");
                }
                uITextureAtlas.material = material;
                uITextureAtlas.padding = r.ReadInt32();
                List<UITextureAtlas.SpriteInfo> list = new List<UITextureAtlas.SpriteInfo>();
                int num3 = r.ReadInt32();
                for (int i = 0; i < num3; i++) {
                    Rect region = default;
                    region.x = r.ReadSingle();
                    region.y = r.ReadSingle();
                    region.width = r.ReadSingle();
                    region.height = r.ReadSingle();
                    string name = r.ReadString();
                    UITextureAtlas.SpriteInfo spriteInfo = new UITextureAtlas.SpriteInfo {
                        name = name,
                        region = region,
                        texture = new Texture2D(Mathf.FloorToInt(num * region.width), Mathf.FloorToInt(num2 * region.height), TextureFormat.ARGB32, false)
                    };
                    spriteInfo.texture.name = name;
                    Color[] pixels2 = texture2D.GetPixels(Mathf.FloorToInt(num * region.xMin),
                                                          Mathf.FloorToInt(num2 * region.yMin),
                                                          Mathf.FloorToInt(num * region.width),
                                                          Mathf.FloorToInt(num2 * region.height));
                    spriteInfo.texture.SetPixels(pixels2);
                    spriteInfo.texture.Apply(false);
                    list.Add(spriteInfo);
                }
                uITextureAtlas.AddSprites(list);
                return uITextureAtlas;
            }
            if (t == typeof(VehicleInfo.Effect)) {
                VehicleInfo.Effect effect;
                effect.m_effect = EffectCollection.FindEffect(r.ReadString());
                effect.m_parkedFlagsForbidden = (VehicleParked.Flags)r.ReadInt32();
                effect.m_parkedFlagsRequired = (VehicleParked.Flags)r.ReadInt32();
                effect.m_vehicleFlagsForbidden = (Vehicle.Flags)r.ReadInt32();
                effect.m_vehicleFlagsRequired = (Vehicle.Flags)r.ReadInt32();
                return effect;
            }
            if (t == typeof(VehicleInfo.VehicleDoor)) {
                VehicleInfo.VehicleDoor vehicleDoor;
                vehicleDoor.m_type = (VehicleInfo.DoorType)r.ReadInt32();
                vehicleDoor.m_location = r.ReadVector3();
                return vehicleDoor;
            }
            if (t == typeof(VehicleInfo.VehicleTrailer)) {
                string name2 = p.packageName + "." + r.ReadString();
                VehicleInfo.VehicleTrailer vehicleTrailer;
                vehicleTrailer.m_info = PrefabCollection<VehicleInfo>.FindLoaded(name2);
                vehicleTrailer.m_probability = r.ReadInt32();
                vehicleTrailer.m_invertProbability = r.ReadInt32();
                return vehicleTrailer;
            }
            if (t == typeof(ManualMilestone) || t == typeof(CombinedMilestone)) {
                return MilestoneCollection.FindMilestone(r.ReadString());
            }
            if (t == typeof(TransportInfo)) {
                return PrefabCollection<TransportInfo>.FindLoaded(r.ReadString());
            }
            if (t == typeof(NetInfo)) {
                Package.Asset asset = p.Find(p.packageMainAsset);
                CustomAssetMetaData customAssetMetaData = asset?.Instantiate<CustomAssetMetaData>();
                if (!(customAssetMetaData is null) && customAssetMetaData.type == CustomAssetMetaData.Type.Road) {
                    return PrefabCollection<NetInfo>.FindLoaded(p.packageName + "." + PackageHelper.StripName(r.ReadString()));
                }
                return PrefabCollection<NetInfo>.FindLoaded(r.ReadString());
            } else {
                if (t == typeof(BuildingInfo.MeshInfo)) {
                    BuildingInfo.MeshInfo meshInfo = new BuildingInfo.MeshInfo();
                    string text2 = r.ReadString();
                    if (text2.Length > 0) {
                        Package.Asset asset2 = p.FindByChecksum(text2);
                        GameObject gameObject = asset2.Instantiate<GameObject>();
                        meshInfo.m_subInfo = gameObject.GetComponent<BuildingInfoBase>();
                        gameObject.SetActive(false);
                        if (!(meshInfo.m_subInfo.m_lodObject is null)) {
                            meshInfo.m_subInfo.m_lodObject.SetActive(false);
                        }
                    } else {
                        meshInfo.m_subInfo = null;
                    }
                    meshInfo.m_flagsForbidden = (Building.Flags)CustomDeserialize(p, typeof(Building.Flags), r);
                    meshInfo.m_flagsRequired = (Building.Flags)CustomDeserialize(p, typeof(Building.Flags), r);
                    meshInfo.m_position = r.ReadVector3();
                    meshInfo.m_angle = r.ReadSingle();
                    return meshInfo;
                }
                if (t == typeof(VehicleInfo.MeshInfo)) {
                    VehicleInfo.MeshInfo meshInfo = new VehicleInfo.MeshInfo();
                    string text3 = r.ReadString();
                    if (text3.Length > 0) {
                        Package.Asset asset3 = p.FindByChecksum(text3);
                        GameObject gameObject = asset3.Instantiate<GameObject>();
                        meshInfo.m_subInfo = gameObject.GetComponent<VehicleInfoBase>();
                        gameObject.SetActive(false);
                        if (!(meshInfo.m_subInfo.m_lodObject is null)) {
                            meshInfo.m_subInfo.m_lodObject.SetActive(false);
                        }
                    } else {
                        meshInfo.m_subInfo = null;
                    }
                    meshInfo.m_vehicleFlagsForbidden = (Vehicle.Flags)CustomDeserialize(p, typeof(Vehicle.Flags), r);
                    meshInfo.m_vehicleFlagsRequired = (Vehicle.Flags)CustomDeserialize(p, typeof(Vehicle.Flags), r);
                    meshInfo.m_parkedFlagsForbidden = (VehicleParked.Flags)CustomDeserialize(p, typeof(VehicleParked.Flags), r);
                    meshInfo.m_parkedFlagsRequired = (VehicleParked.Flags)CustomDeserialize(p, typeof(VehicleParked.Flags), r);
                    return meshInfo;
                }
                if (t == typeof(Building.Flags)) {
                    return (Building.Flags)r.ReadInt32();
                }
                if (t == typeof(Vehicle.Flags)) {
                    return (Vehicle.Flags)r.ReadInt32();
                }
                if (t == typeof(VehicleParked.Flags)) {
                    return (VehicleParked.Flags)r.ReadInt32();
                }
                if (t == typeof(DepotAI.SpawnPoint)) {
                    return new DepotAI.SpawnPoint {
                        m_position = r.ReadVector3(),
                        m_target = r.ReadVector3()
                    };
                }
                if (t == typeof(PropInfo.Effect)) {
                    return new PropInfo.Effect {
                        m_effect = EffectCollection.FindEffect(r.ReadString()),
                        m_position = r.ReadVector3(),
                        m_direction = r.ReadVector3()
                    };
                }
                if (t == typeof(BuildingInfo.SubInfo)) {
                    BuildingInfo.SubInfo subInfo = new BuildingInfo.SubInfo();
                    string text4 = r.ReadString();
                    subInfo.m_buildingInfo = PrefabCollection<BuildingInfo>.FindLoaded(p.packageName + "." + text4);
                    if (subInfo.m_buildingInfo is null) {
                        subInfo.m_buildingInfo = PrefabCollection<BuildingInfo>.FindLoaded(text4);
                    }
                    subInfo.m_position = r.ReadVector3();
                    subInfo.m_angle = r.ReadSingle();
                    subInfo.m_fixedHeight = r.ReadBoolean();
                    return subInfo;
                }
                if (t == typeof(PropInfo.ParkingSpace)) {
                    return new PropInfo.ParkingSpace {
                        m_position = r.ReadVector3(),
                        m_direction = r.ReadVector3(),
                        m_size = r.ReadVector3()
                    };
                }
                if (t == typeof(PropInfo.SpecialPlace)) {
                    return new PropInfo.SpecialPlace {
                        m_specialFlags = (CitizenInstance.Flags)r.ReadInt32(),
                        m_position = r.ReadVector3(),
                        m_direction = r.ReadVector3()
                    };
                }
                if (t == typeof(NetInfo.Lane)) {
                    return new NetInfo.Lane {
                        m_position = r.ReadSingle(),
                        m_width = r.ReadSingle(),
                        m_verticalOffset = r.ReadSingle(),
                        m_stopOffset = r.ReadSingle(),
                        m_speedLimit = r.ReadSingle(),
                        m_direction = (NetInfo.Direction)r.ReadInt32(),
                        m_laneType = (NetInfo.LaneType)r.ReadInt32(),
                        m_vehicleType = (VehicleInfo.VehicleType)r.ReadInt32(),
                        m_stopType = (VehicleInfo.VehicleType)r.ReadInt32(),
                        m_laneProps = (NetLaneProps)CustomDeserialize(p, typeof(NetLaneProps), r),
                        m_allowConnect = r.ReadBoolean(),
                        m_useTerrainHeight = r.ReadBoolean(),
                        m_centerPlatform = r.ReadBoolean(),
                        m_elevated = r.ReadBoolean()
                    };
                }
                if (t == typeof(NetLaneProps)) {
                    NetLaneProps netLaneProps = ScriptableObject.CreateInstance<NetLaneProps>();
                    int num4 = r.ReadInt32();
                    netLaneProps.m_props = new NetLaneProps.Prop[num4];
                    for (int j = 0; j < num4; j++) {
                        object obj = CustomDeserialize(p, typeof(NetLaneProps.Prop), r);
                        netLaneProps.m_props[j] = obj is null ? null : (NetLaneProps.Prop)obj;
                    }
                    return netLaneProps;
                }
                if (t == typeof(NetLaneProps.Prop)) {
                    NetLaneProps.Prop prop = new NetLaneProps.Prop {
                        m_flagsRequired = (NetLane.Flags)r.ReadInt32(),
                        m_flagsForbidden = (NetLane.Flags)r.ReadInt32(),
                        m_startFlagsRequired = (NetNode.Flags)r.ReadInt32(),
                        m_startFlagsForbidden = (NetNode.Flags)r.ReadInt32(),
                        m_endFlagsRequired = (NetNode.Flags)r.ReadInt32(),
                        m_endFlagsForbidden = (NetNode.Flags)r.ReadInt32(),
                        m_colorMode = (NetLaneProps.ColorMode)r.ReadInt32(),
                        m_prop = PrefabCollection<PropInfo>.FindLoaded(r.ReadString()),
                        m_tree = PrefabCollection<TreeInfo>.FindLoaded(r.ReadString()),
                        m_position = r.ReadVector3(),
                        m_angle = r.ReadSingle(),
                        m_segmentOffset = r.ReadSingle(),
                        m_repeatDistance = r.ReadSingle(),
                        m_minLength = r.ReadSingle(),
                        m_cornerAngle = r.ReadSingle(),
                        m_probability = r.ReadInt32()
                    };
                    if (p.version >= 8) {
                        prop.m_upgradable = r.ReadBoolean();
                    } else {
                        prop.m_upgradable = (!(prop.m_tree is null) && prop.m_repeatDistance > 0f);
                    }
                    return prop;
                }
                if (t == typeof(NetInfo.Segment)) {
                    NetInfo.Segment segment = new NetInfo.Segment {
                        m_mesh = r.ReadMesh(),
                        m_material = r.ReadMaterial(),
                        m_lodMesh = r.ReadMesh(),
                        m_lodMaterial = r.ReadMaterial(),
                        m_forwardRequired = (NetSegment.Flags)r.ReadInt32(),
                        m_forwardForbidden = (NetSegment.Flags)r.ReadInt32(),
                        m_backwardRequired = (NetSegment.Flags)r.ReadInt32(),
                        m_backwardForbidden = (NetSegment.Flags)r.ReadInt32(),
                        m_emptyTransparent = r.ReadBoolean(),
                        m_disableBendNodes = r.ReadBoolean()
                    };
                    return segment;
                }
                if (t == typeof(NetInfo.Node)) {
                    NetInfo.Node node = new NetInfo.Node {
                        m_mesh = r.ReadMesh(),
                        m_material = r.ReadMaterial(),
                        m_lodMesh = r.ReadMesh(),
                        m_lodMaterial = r.ReadMaterial(),
                        m_flagsRequired = (NetNode.Flags)r.ReadInt32(),
                        m_flagsForbidden = (NetNode.Flags)r.ReadInt32(),
                        m_connectGroup = (NetInfo.ConnectGroup)r.ReadInt32(),
                        m_directConnect = r.ReadBoolean(),
                        m_emptyTransparent = r.ReadBoolean()
                    };
                    return node;
                }
                if (t == typeof(BuildingInfo)) {
                    Package.Asset asset12 = p.Find(p.packageMainAsset);
                    CustomAssetMetaData customAssetMetaData2 = asset12?.Instantiate<CustomAssetMetaData>();
                    if (!(customAssetMetaData2 is null) && customAssetMetaData2.type == CustomAssetMetaData.Type.Road) {
                        return PrefabCollection<BuildingInfo>.FindLoaded(p.packageName + "." + StripName(r.ReadString()));
                    }
                    return PrefabCollection<BuildingInfo>.FindLoaded(r.ReadString());
                } else {
                    if (t == typeof(Dictionary<string, byte[]>)) {
                        int num5 = r.ReadInt32();
                        Dictionary<string, byte[]> dictionary = new Dictionary<string, byte[]>();
                        for (int k = 0; k < num5; k++) {
                            string key = r.ReadString();
                            int count = r.ReadInt32();
                            byte[] value = r.ReadBytes(count);
                            dictionary[key] = value;
                        }
                        return dictionary;
                    }
                    return null;
                }
            }
        }

        internal static object Deserialize(Package package, PackageReader reader) {
            if (!DeserializeHeader(out Type type, reader)) {
                return null;
            }
            if (type == typeof(GameObject)) {
                return DeserializeGameObject(package, reader);
            }
            if (type == typeof(Mesh)) {
                return DeserializeMesh(package, reader);
            }
            if (type == typeof(Material)) {
                return DeserializeMaterial(package, reader);
            }
            if (type == typeof(Texture2D) || type == typeof(Image)) {
                return DeserializeTexture(package, reader);
            }
            if (typeof(ScriptableObject).IsAssignableFrom(type)) {
                return DeserializeScriptableObject(package, type, reader);
            }
            return DeserializeObject(package, type, reader);
        }

        internal static bool DeserializeHeader(out Type type, PackageReader reader) {
            type = null;
            if (reader.ReadBoolean()) {
                return false;
            }
            string text = reader.ReadString();
            type = Type.GetType(text);
            if (type is null) {
                type = Type.GetType(ResolveLegacyType(text));
                if (type is null) {
                    if (HandleUnknownType(text, reader) < 0) {
                        throw new InvalidDataException("Unknown type to deserialize " + text);
                    }
                    return false;
                }
            }
            return true;
        }

        internal static bool DeserializeHeader(out Type type, out string name, PackageReader reader) {
            type = null;
            name = null;
            if (reader.ReadBoolean()) {
                return false;
            }
            string text = reader.ReadString();
            type = Type.GetType(text);
            name = reader.ReadString();
            if (type is null) {
                type = Type.GetType(ResolveLegacyType(text));
                if (type is null) {
                    if (HandleUnknownType(text, reader) < 0) {
                        throw new InvalidDataException("Unknown type to deserialize " + text);
                    }
                    return false;
                }
            }
            return true;
        }

        internal static object DeserializeSingleObject(Package package, Type type, PackageReader reader, Type expectedType) {
            if (!(LoaderManager.GetCustomDeserializeHandler() is null)) {
                object obj = CustomDeserialize(package, type, reader);
                if (!(obj is null)) {
                    return obj;
                }
            }
            if (typeof(ScriptableObject).IsAssignableFrom(type)) {
                return reader.ReadAsset(package).Instantiate();
            }
            if (typeof(GameObject).IsAssignableFrom(type)) {
                return reader.ReadAsset(package).Instantiate();
            }
            if (!IsUnityType(type)) {
                Debug.Log("Unsupported type for deserialization: [" + type.Name + "]");
                return null;
            }
            if (package.version < 3 && !(expectedType is null) && expectedType == typeof(Package.Asset)) {
                return reader.ReadUnityType(expectedType, null);
            }
            return reader.ReadUnityType(type, package);
        }

        internal static Object DeserializeScriptableObject(Package package, Type type, PackageReader reader) {
            if (!(LoaderManager.GetCustomDeserializeHandler() is null)) {
                object obj = CustomDeserialize(package, type, reader);
                if (!(obj is null)) {
                    return (Object)obj;
                }
            }
            ScriptableObject scriptableObject = ScriptableObject.CreateInstance(type);
            scriptableObject.name = reader.ReadString();
            int num = reader.ReadInt32();
            for (int i = 0; i < num; i++) {
                if (DeserializeHeader(out Type type2, out string name, reader)) {
                    FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    Type expectedType = field?.FieldType;
                    if (type2.IsArray) {
                        int num2 = reader.ReadInt32();
                        Array array = Array.CreateInstance(type2.GetElementType(), num2);
                        for (int j = 0; j < num2; j++) {
                            array.SetValue(DeserializeSingleObject(package, type2.GetElementType(), reader, expectedType), j);
                        }
                        if (!(field is null)) {
                            field.SetValue(scriptableObject, array);
                        }
                    } else {
                        object value = DeserializeSingleObject(package, type2, reader, expectedType);
                        if (!(field is null)) {
                            field.SetValue(scriptableObject, value);
                        }
                    }
                }
            }
            return scriptableObject;
        }

        internal static Object DeserializeGameObject(Package package, PackageReader reader) {
            string name = reader.ReadString();
            GameObject gameObject = new GameObject(name) {
                tag = reader.ReadString(),
                layer = reader.ReadInt32()
            };
            gameObject.SetActive(reader.ReadBoolean());
            int num = reader.ReadInt32();
            for (int i = 0; i < num; i++) {
                DeserializeComponent(package, gameObject, reader);
            }
            return gameObject;
        }

        internal static void DeserializeComponent(Package package, GameObject go, PackageReader reader) {
            if (!DeserializeHeader(out Type type, reader)) {
                return;
            }
            if (type == typeof(Transform)) {
                DeserializeTransform(package, go.transform, reader);
                return;
            }
            if (type == typeof(MeshFilter)) {
                DeserializeMeshFilter(package, go.AddComponent(type) as MeshFilter, reader);
                return;
            }
            if (type == typeof(MeshRenderer)) {
                DeserializeMeshRenderer(package, go.AddComponent(type) as MeshRenderer, reader);
                return;
            }
            if (type == typeof(SkinnedMeshRenderer)) {
                DeserializeSkinnedMeshRenderer(package, go.AddComponent(type) as SkinnedMeshRenderer, reader);
                return;
            }
            if (type == typeof(Animator)) {
                DeserializeAnimator(package, go.AddComponent(type) as Animator, reader);
                return;
            }
            if (typeof(MonoBehaviour).IsAssignableFrom(type)) {
                DeserializeMonoBehaviour(package, (MonoBehaviour)go.AddComponent(type), reader);
                return;
            }
            throw new InvalidDataException("Unknown type to deserialize " + type.Name);
        }

        internal static void DeserializeAnimator(Package _, Animator animator, PackageReader reader) {
            animator.applyRootMotion = reader.ReadBoolean();
            animator.updateMode = (AnimatorUpdateMode)reader.ReadInt32();
            animator.cullingMode = (AnimatorCullingMode)reader.ReadInt32();
        }

        internal static Object DeserializeTexture(Package package, PackageReader reader) {
            string name = reader.ReadString();
            bool linear = reader.ReadBoolean();
            int anisoLevel = 1;
            if (package.version >= 6) {
                anisoLevel = reader.ReadInt32();
            }
            int count = reader.ReadInt32();
            byte[] fileByte = reader.ReadBytes(count);
            Image image = new Image(fileByte);
            Texture2D texture2D = image.CreateTexture(linear);
            texture2D.name = name;
            texture2D.anisoLevel = anisoLevel;
            return texture2D;
        }

        internal static Object DeserializeMaterial(Package package, PackageReader reader) {
            string name = reader.ReadString();
            string name2 = reader.ReadString();
            Material material = new Material(Shader.Find(name2)) {
                name = name
            };
            int num = reader.ReadInt32();
            for (int i = 0; i < num; i++) {
                int num2 = reader.ReadInt32();
                if (num2 == 0) {
                    material.SetColor(reader.ReadString(), reader.ReadColor());
                } else if (num2 == 1) {
                    material.SetVector(reader.ReadString(), reader.ReadVector4());
                } else if (num2 == 2) {
                    material.SetFloat(reader.ReadString(), reader.ReadSingle());
                } else if (num2 == 3) {
                    string name3 = reader.ReadString();
                    if (!reader.ReadBoolean()) {
                        material.SetTexture(name3, reader.ReadTexture(package));
                        //material.SetTexture(name3, reader.ReadAsset(package).Instantiate<Texture>());
                    } else {
                        material.SetTexture(name3, null);
                    }
                }
            }
            return material;
        }

        internal static void DeserializeTransform(Package _, Transform transform, PackageReader reader) {
            transform.localPosition = reader.ReadVector3();
            transform.localRotation = reader.ReadQuaternion();
            transform.localScale = reader.ReadVector3();
        }

        internal static void DeserializeMeshFilter(Package package, MeshFilter meshFilter, PackageReader reader) =>
            meshFilter.sharedMesh = reader.ReadMesh(package);// ReadAsset(package).Instantiate<Mesh>();

        internal static void DeserializeMonoBehaviour(Package package, MonoBehaviour behaviour, PackageReader reader) {
            int i, j;
            int num = reader.ReadInt32();
            for (i = 0; i < num; i++) {
                if (DeserializeHeader(out Type type, out string name, reader)) {
                    FieldInfo field = behaviour.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    Type expectedType = field?.FieldType;
                    if (type.IsArray) {
                        int len = reader.ReadInt32();
                        Type elementType = type.GetElementType();
                        switch(Type.GetTypeCode(elementType)) {
                        case TypeCode.Single:
                            float[] f = new float[len];
                            for(j = 0; j < f.Length; j++) {
                                f[j] = reader.ReadSingle();
                            }
                            field?.SetValue(behaviour, f);
                            break;
                        case TypeCode.Int32:
                            int[] iarray = new int[len];
                            for (j = 0; j < iarray.Length; j++) {
                                iarray[j] = reader.ReadInt32();
                            }
                            field?.SetValue(behaviour, iarray);
                            break;
                        default:
                            if (elementType == typeof(Vector2)) {
                                Vector2[] array = new Vector2[len];
                                for (j = 0; j < array.Length; j++) {
                                    array[j] = reader.ReadVector2();
                                }
                                field?.SetValue(behaviour, array);
                            } else if (elementType == typeof(Vector3)) {
                                Vector3[] array = new Vector3[len];
                                for (j = 0; j < array.Length; j++) {
                                    array[j] = reader.ReadVector3();
                                }
                                field?.SetValue(behaviour, array);
                            } else {
                                Array array = Array.CreateInstance(type.GetElementType(), len);
                                for (j = 0; j < array.Length; j++) {
                                    array.SetValue(DeserializeSingleObject(package, elementType, reader, expectedType), j);
                                }
                                field?.SetValue(behaviour, array);
                            }
                            break;
                        }
                    } else {
                        switch(Type.GetTypeCode(type)) {
                        case TypeCode.Boolean:
                            field?.SetValue(behaviour, reader.ReadBoolean());
                            break;
                        case TypeCode.Byte:
                            field?.SetValue(behaviour, reader.ReadByte());
                            break;
                        case TypeCode.SByte:
                            field?.SetValue(behaviour, reader.ReadSByte());
                            break;
                        case TypeCode.Int16:
                            field?.SetValue(behaviour, reader.ReadInt16());
                            break;
                        case TypeCode.UInt16:
                            field?.SetValue(behaviour, reader.ReadUInt16());
                            break;
                        case TypeCode.Int32:
                            field?.SetValue(behaviour, reader.ReadInt32());
                            break;
                        case TypeCode.UInt32:
                            field?.SetValue(behaviour, reader.ReadUInt32());
                            break;
                        case TypeCode.Single:
                            field?.SetValue(behaviour, reader.ReadSingle());
                            break;
                        case TypeCode.Int64:
                            field?.SetValue(behaviour, reader.ReadInt64());
                            break;
                        case TypeCode.UInt64:
                            field?.SetValue(behaviour, reader.ReadUInt64());
                            break;
                        default:
                            field?.SetValue(behaviour, DeserializeSingleObject(package, type, reader, expectedType));
                            break;
                        }
                    }
                }
            }
        }

        internal static object DeserializeObject(Package package, Type type, PackageReader reader) {
            int i, j;
            if (!(LoaderManager.GetCustomDeserializeHandler() is null)) {
                object obj = CustomDeserialize(package, type, reader);
                if (!(obj is null)) {
                    return obj;
                }
            }
            object obj2 = Activator.CreateInstance(type);
            reader.ReadString();
            int num = reader.ReadInt32();
            for (i = 0; i < num; i++) {
                if (DeserializeHeader(out Type type2, out string text, reader)) {
                    FieldInfo field = type.GetField(text, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (field is null) {
                        text = ResolveLegacyMember(type2, type, text);
                        field = type.GetField(text, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    }
                    Type expectedType = field?.FieldType;
                    if (type2.IsArray) {
                        int len = reader.ReadInt32();
                        Type elementType = type2.GetElementType();
                        switch (Type.GetTypeCode(elementType)) {
                        case TypeCode.Single:
                            float[] f = new float[len];
                            for (j = 0; j < f.Length; j++) {
                                f[j] = reader.ReadSingle();
                            }
                            field?.SetValue(obj2, f);
                            break;
                        case TypeCode.Int32:
                            int[] iarray = new int[len];
                            for (j = 0; j < iarray.Length; j++) {
                                iarray[j] = reader.ReadInt32();
                            }
                            field?.SetValue(obj2, iarray);
                            break;
                        default:
                            if (elementType == typeof(Vector2)) {
                                Vector2[] array = new Vector2[len];
                                for (j = 0; j < array.Length; j++) {
                                    array[j] = reader.ReadVector2();
                                }
                                field?.SetValue(obj2, array);
                            } else if (elementType == typeof(Vector3)) {
                                Vector3[] array = new Vector3[len];
                                for (j = 0; j < array.Length; j++) {
                                    array[j] = reader.ReadVector3();
                                }
                                field?.SetValue(obj2, array);
                            } else {
                                Array array = Array.CreateInstance(type2.GetElementType(), len);
                                for (j = 0; j < array.Length; j++) {
                                    array.SetValue(DeserializeSingleObject(package, elementType, reader, expectedType), j);
                                }
                                field?.SetValue(obj2, array);
                            }
                            break;
                        }
                    } else {
                        switch (Type.GetTypeCode(type2)) {
                        case TypeCode.Boolean:
                            field?.SetValue(obj2, reader.ReadBoolean());
                            break;
                        case TypeCode.Byte:
                            field?.SetValue(obj2, reader.ReadByte());
                            break;
                        case TypeCode.SByte:
                            field?.SetValue(obj2, reader.ReadSByte());
                            break;
                        case TypeCode.Int16:
                            field?.SetValue(obj2, reader.ReadInt16());
                            break;
                        case TypeCode.UInt16:
                            field?.SetValue(obj2, reader.ReadUInt16());
                            break;
                        case TypeCode.Int32:
                            field?.SetValue(obj2, reader.ReadInt32());
                            break;
                        case TypeCode.UInt32:
                            field?.SetValue(obj2, reader.ReadUInt32());
                            break;
                        case TypeCode.Single:
                            field?.SetValue(obj2, reader.ReadSingle());
                            break;
                        case TypeCode.Int64:
                            field?.SetValue(obj2, reader.ReadInt64());
                            break;
                        case TypeCode.UInt64:
                            field?.SetValue(obj2, reader.ReadUInt64());
                            break;
                        default:
                            field?.SetValue(obj2, DeserializeSingleObject(package, type2, reader, expectedType));
                            break;
                        }
                    }
                }
            }
            return obj2;
        }

        internal static void DeserializeMeshRenderer(Package package, MeshRenderer renderer, PackageReader reader) {
            int num = reader.ReadInt32();
            Material[] array = new Material[num];
            for (int i = 0; i < num; i++) {
                array[i] = reader.ReadMaterial(package); // reader.ReadAsset(package).Instantiate<Material>();
            }
            renderer.sharedMaterials = array;
        }

        internal static void DeserializeSkinnedMeshRenderer(Package package, SkinnedMeshRenderer smr, PackageReader reader) {
            int num = reader.ReadInt32();
            Material[] array = new Material[num];
            for (int i = 0; i < num; i++) {
                array[i] = reader.ReadMaterial(package); // reader.ReadAsset(package).Instantiate<Material>();
            }
            smr.sharedMaterials = array;
            smr.sharedMesh = reader.ReadMesh(package); // reader.ReadAsset(package).Instantiate<Mesh>();
        }

        internal static Object DeserializeMesh(Package _, PackageReader reader) {
            Mesh mesh = new Mesh {
                name = reader.ReadString(),
                vertices = reader.ReadVector3Array(),
                colors = reader.ReadColorArray(),
                uv = reader.ReadVector2Array(),
                normals = reader.ReadVector3Array(),
                tangents = reader.ReadVector4Array(),
                boneWeights = reader.ReadBoneWeightsArray(),
                bindposes = reader.ReadMatrix4x4Array(),
                subMeshCount = reader.ReadInt32()
            };
            for (int i = 0; i < mesh.subMeshCount; i++) {
                mesh.SetTriangles(reader.ReadInt32Array(), i);
            }
            return mesh;
        }

        private static int HandleUnknownType(string type, PackageReader reader) {
            int num = HandleUnknownType(type);
            if (num > 0) {
                reader.ReadBytes(num);
                return num;
            }
            return -1;
        }

        private static int UnknownTypeHandler(string type) {
            if (type == "UnlockManager+Milestone, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null") {
                return 4;
            }
            return -1;
        }

        internal static int HandleUnknownType(string type) {
            int num = UnknownTypeHandler(type);
            CODebugBase<InternalLogChannel>.Warn(InternalLogChannel.Packer, string.Concat(new object[] {
                "Unexpected type '",
                type,
                "' detected. No resolver handled this type. Skipping ",
                num,
                " bytes."
            }));
            return num;
        }

        private static string ResolveLegacyTypeHandler(string type) {
            if (type == "LoadSaveMapPanel+MapMetaData, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null") {
                return "MapMetaData, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
            }
            if (type == "LoadSavePanelBase+MapMetaData, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null") {
                return "MapMetaData, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
            }
            if (type == "LoadSavePanelBase+SystemMapMetaData, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null") {
                return "SystemMapMetaData, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
            }
            if (type == "LoadSavePanelBase+SaveGameMetaData, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null") {
                return "SaveGameMetaData, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
            }
            return type;
        }

        internal static string ResolveLegacyType(string type) {
            string text = ResolveLegacyTypeHandler(type);
            CODebugBase<InternalLogChannel>.Warn(InternalLogChannel.Packer, string.Concat(new string[] {
                "Unkown type detected. Attempting to resolve from '",
                type,
                "' to '",
                text,
                "'"
            }));
            return text;
        }

        private static string ResolveLegacyMemberHandler(Type type, string name) {
            if ((type == typeof(SystemMapMetaData) || type == typeof(MapMetaData) || type == typeof(SaveGameMetaData)) && name == "saveRef") {
                return "assetRef";
            }
            return name;
        }

        internal static string ResolveLegacyMember(Type fieldType, Type classType, string member) {
            string text = ResolveLegacyMemberHandler(classType, member);
            CODebugBase<InternalLogChannel>.Warn(InternalLogChannel.Packer, string.Concat(new string[] {
                "Unkown member detected of type ",
                fieldType.FullName,
                " in ",
                classType.FullName,
                ". Attempting to resolve from '",
                member,
                "' to '",
                text,
                "'"
            }));
            return text;
        }
    }
}
