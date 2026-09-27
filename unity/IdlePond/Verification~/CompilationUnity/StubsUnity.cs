// Signatures minimales de l'API Unity 6 utilisées par Interface/ et Editeur/.
// Aucun comportement : seulement de quoi compiler. Voir le .csproj.
#pragma warning disable
using System;

namespace UnityEngine
{
    public class Object
    {
        public string name { get; set; }
        public static T FindAnyObjectByType<T>() where T : Object => null;
        public static void Destroy(Object objet) { }
        public static void DontDestroyOnLoad(Object objet) { }
    }

    public class Component : Object
    {
        public GameObject gameObject => null;
        public Transform transform => null;
    }

    public class Behaviour : Component { }
    public class MonoBehaviour : Behaviour { }
    public class ScriptableObject : Object { public static T CreateInstance<T>() where T : ScriptableObject => null; }

    public sealed class GameObject : Object
    {
        public GameObject(string nom) { }
        public T AddComponent<T>() where T : Component => null;
        public void SetActive(bool actif) { }
        public Transform transform => null;
        public string tag { get; set; }
    }

    public class Transform : Component { public void SetParent(Transform parent, bool garderLaPosition) { } }

    public enum CameraClearFlags { Skybox, SolidColor, Depth, Nothing }

    public sealed class Camera : Behaviour
    {
        public static Camera main => null;
        public CameraClearFlags clearFlags { get; set; }
        public Color backgroundColor { get; set; }
        public int cullingMask { get; set; }
        public bool orthographic { get; set; }
    }

    public struct Color { public static Color black => default; }
    public static class ColorUtility { public static bool TryParseHtmlString(string html, out Color couleur) { couleur = default; return true; } }

    public struct Vector2 { public float x, y; }
    public struct Vector2Int { public Vector2Int(int x, int y) { this.x = x; this.y = y; } public int x, y; }

    public struct Rect : IEquatable<Rect>
    {
        public float xMin, xMax, yMin, yMax, width, height;
        public bool Equals(Rect autre) => false;
        public static bool operator ==(Rect a, Rect b) => a.Equals(b);
        public static bool operator !=(Rect a, Rect b) => !a.Equals(b);
        public override bool Equals(object o) => false;
        public override int GetHashCode() => 0;
    }

    public static class Screen
    {
        public static Rect safeArea => default;
        public static int width => 0;
        public static int height => 0;
    }

    public static class Application
    {
        public static string persistentDataPath => "";
        public static string dataPath => "";
        public static bool isPlaying => false;
        public static bool isBatchMode => false;
        public static bool runInBackground { get; set; }
        public static int targetFrameRate { get; set; }
    }

    public static class Time { public static float unscaledDeltaTime => 0; }

    public static class Debug
    {
        public static void Log(object message) { }
        public static void LogWarning(object message) { }
    }

    public static class Mathf { public static int Clamp(int valeur, int min, int max) => valeur; }

    public sealed class Font : Object { }

    public static class Resources
    {
        public static T Load<T>(string chemin) where T : Object => null;
        public static T GetBuiltinResource<T>(string chemin) where T : Object => null;
    }

    public static class PlayerPrefs
    {
        public static bool HasKey(string clef) => false;
        public static string GetString(string clef) => null;
        public static void SetString(string clef, string valeur) { }
        public static void DeleteKey(string clef) { }
        public static void Save() { }
    }

    public enum RuntimeInitializeLoadType { AfterSceneLoad, BeforeSceneLoad }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RuntimeInitializeOnLoadMethodAttribute : Attribute { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType type) { } }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class TooltipAttribute : Attribute { public TooltipAttribute(string texte) { } }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SerializeField : Attribute { }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DisallowMultipleComponent : Attribute { }

    public class GUIContent
    {
        public GUIContent(string texte) { }
        public GUIContent(string texte, string infobulle) { }
    }

    public class GUIStyle { }
    public sealed class GUILayoutOption { }

    public static class GUILayout
    {
        public static void Label(string texte, params GUILayoutOption[] options) { }
        public static void Label(string texte, GUIStyle style, params GUILayoutOption[] options) { }
        public static bool Button(string texte, params GUILayoutOption[] options) => false;
        public static GUILayoutOption Width(float largeur) => null;
        public static GUILayoutOption Height(float hauteur) => null;
    }
}

namespace UnityEngine.SceneManagement
{
    public struct Scene { }
}

namespace UnityEngine.UIElements
{
    public enum DisplayStyle { Flex, None }
    public enum PickingMode { Position, Ignore }
    public enum ScrollViewMode { Vertical, Horizontal, VerticalAndHorizontal }
    public enum PanelScaleMode { ConstantPixelSize, ConstantPhysicalSize, ScaleWithScreenSize }
    public enum PanelScreenMatchMode { MatchWidthOrHeight, Shrink, Expand }

    public struct Length { public static Length Percent(float valeur) => default; }

    public struct StyleLength
    {
        public static implicit operator StyleLength(float valeur) => default;
        public static implicit operator StyleLength(Length valeur) => default;
    }

    public struct StyleFloat { public static implicit operator StyleFloat(float valeur) => default; }

    public struct StyleEnum<T> where T : struct { public static implicit operator StyleEnum<T>(T valeur) => default; }

    public struct FontDefinition { public static FontDefinition FromFont(Font police) => default; }

    public struct StyleFontDefinition { public StyleFontDefinition(FontDefinition definition) { } }

    public interface IStyle
    {
        StyleLength width { get; set; }
        StyleLength marginTop { get; set; }
        StyleLength paddingLeft { get; set; }
        StyleLength paddingRight { get; set; }
        StyleLength paddingTop { get; set; }
        StyleLength paddingBottom { get; set; }
        StyleFloat flexGrow { get; set; }
        StyleEnum<DisplayStyle> display { get; set; }
        StyleFontDefinition unityFontDefinition { get; set; }
    }

    public class StyleSheet : ScriptableObject { }
    public class ThemeStyleSheet : StyleSheet { }

    public struct VisualElementStyleSheetSet { public void Add(StyleSheet feuille) { } }

    public interface IVisualElementScheduledItem { IVisualElementScheduledItem StartingIn(long millisecondes); }
    public interface IVisualElementScheduler { IVisualElementScheduledItem Execute(Action action); }

    public delegate void EventCallback<in TEvent>(TEvent evenement);
    public abstract class EventBase { }
    public abstract class EventBase<T> : EventBase where T : EventBase<T>, new() { }
    public sealed class GeometryChangedEvent : EventBase<GeometryChangedEvent> { public Rect newRect => default; }

    public class VisualElement
    {
        public IStyle style => null;
        public VisualElementStyleSheetSet styleSheets => default;
        public PickingMode pickingMode { get; set; }
        public IVisualElementScheduler schedule => null;
        public Rect layout => default;
        public VisualElement parent => null;
        public void Add(VisualElement enfant) { }
        public void Clear() { }
        public void RemoveFromHierarchy() { }
        public void AddToClassList(string classe) { }
        public void RemoveFromClassList(string classe) { }
        public void EnableInClassList(string classe, bool actif) { }
        public void SetEnabled(bool actif) { }
        public void RegisterCallback<TEventType>(EventCallback<TEventType> rappel) where TEventType : EventBase<TEventType>, new() { }
    }

    public class TextElement : VisualElement { public string text { get; set; } }

    public class Label : TextElement
    {
        public Label() { }
        public Label(string texte) { }
    }

    public class Button : TextElement
    {
        public Button() { }
        public Button(Action clic) { }
        public event Action clicked;
    }

    public class ScrollView : VisualElement { public ScrollView(ScrollViewMode mode) { } }

    public sealed class PanelSettings : ScriptableObject
    {
        public PanelScaleMode scaleMode { get; set; }
        public Vector2Int referenceResolution { get; set; }
        public PanelScreenMatchMode screenMatchMode { get; set; }
        public float match { get; set; }
        public ThemeStyleSheet themeStyleSheet { get; set; }
    }

    public sealed class UIDocument : MonoBehaviour
    {
        public PanelSettings panelSettings { get; set; }
        public VisualElement rootVisualElement => null;
    }
}

namespace UnityEditor
{
    using UnityEngine;

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class MenuItem : Attribute
    {
        public MenuItem(string chemin) { }
        public int priority;
    }

    public class EditorWindow : ScriptableObject
    {
        public static T GetWindow<T>(string titre) where T : EditorWindow => null;
        public void ShowNotification(GUIContent contenu) { }
    }

    public enum MessageType { None, Info, Warning, Error }

    public static class EditorStyles { public static GUIStyle boldLabel => null; }

    public static class EditorGUILayout
    {
        public static int IntField(string libelle, int valeur, params GUILayoutOption[] options) => valeur;
        public static bool Toggle(GUIContent libelle, bool valeur, params GUILayoutOption[] options) => valeur;
        public static float Slider(string libelle, float valeur, float min, float max, params GUILayoutOption[] options) => valeur;
        public static float Slider(GUIContent libelle, float valeur, float min, float max, params GUILayoutOption[] options) => valeur;
        public static void LabelField(string libelle, GUIStyle style, params GUILayoutOption[] options) { }
        public static void LabelField(string libelle, string texte, params GUILayoutOption[] options) { }
        public static void Space() { }
        public static void HelpBox(string message, MessageType type) { }
        public static Vector2 BeginScrollView(Vector2 position, params GUILayoutOption[] options) => position;
        public static void EndScrollView() { }

        public sealed class HorizontalScope : IDisposable
        {
            public HorizontalScope(params GUILayoutOption[] options) { }
            public void Dispose() { }
        }
    }

    public static class EditorGUI
    {
        public struct DisabledScope : IDisposable
        {
            public DisabledScope(bool desactive) { }
            public void Dispose() { }
        }
    }

    public static class EditorUtility
    {
        public static bool DisplayDialog(string titre, string message, string ok) => true;
        public static bool DisplayDialog(string titre, string message, string ok, string annuler) => true;
        public static void RevealInFinder(string chemin) { }
        public static void DisplayProgressBar(string titre, string info, float progression) { }
        public static void ClearProgressBar() { }
    }

    public static class EditorApplication
    {
        public static double timeSinceStartup => 0;
        public static void Exit(int code) { }
    }

    public static class EditorGUIUtility { public static string systemCopyBuffer { get; set; } }

    public static class AssetDatabase
    {
        public static bool IsValidFolder(string chemin) => false;
        public static string CreateFolder(string parent, string nom) => "";
        public static void SaveAssets() { }
    }

    public sealed class EditorBuildSettingsScene
    {
        public EditorBuildSettingsScene(string chemin, bool active) { }
        public string path { get; set; }
    }

    public static class EditorBuildSettings { public static EditorBuildSettingsScene[] scenes { get; set; } }

    public enum UIOrientation { Portrait, PortraitUpsideDown, LandscapeRight, LandscapeLeft, AutoRotation }

    public static class PlayerSettings
    {
        public static string productName { get; set; }
        public static string companyName { get; set; }
        public static bool runInBackground { get; set; }
        public static UIOrientation defaultInterfaceOrientation { get; set; }
        public static void SetApplicationIdentifier(Build.NamedBuildTarget cible, string identifiant) { }
    }

    public enum BuildTarget { StandaloneOSX, StandaloneWindows64, StandaloneLinux64, iOS, Android, WebGL }

    [Flags]
    public enum BuildOptions { None = 0 }

    public struct BuildPlayerOptions
    {
        public string[] scenes { get; set; }
        public string locationPathName { get; set; }
        public BuildTarget target { get; set; }
        public BuildOptions options { get; set; }
    }

    public static class BuildPipeline { public static Build.Reporting.BuildReport BuildPlayer(BuildPlayerOptions options) => null; }
}

namespace UnityEditor.Build
{
    public struct NamedBuildTarget
    {
        public static NamedBuildTarget Standalone => default;
        public static NamedBuildTarget Android => default;
        public static NamedBuildTarget iOS => default;
        public static NamedBuildTarget WebGL => default;
    }

    public class BuildFailedException : Exception { public BuildFailedException(string message) : base(message) { } }
}

namespace UnityEditor.Build.Reporting
{
    public enum BuildResult { Unknown, Succeeded, Failed, Cancelled }
    public struct BuildSummary { public BuildResult result => default; public ulong totalSize => 0; }
    public sealed class BuildReport { public BuildSummary summary => default; }
}

namespace UnityEditor.SceneManagement
{
    using UnityEngine.SceneManagement;

    public enum NewSceneSetup { EmptyScene, DefaultGameObjects }
    public enum NewSceneMode { Single, Additive }

    public static class EditorSceneManager
    {
        public static Scene NewScene(NewSceneSetup setup, NewSceneMode mode) => default;
        public static bool SaveScene(Scene scene, string chemin) => true;
    }
}
