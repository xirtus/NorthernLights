﻿using AuroraEngine;
using NAudio.MediaFoundation;
using System.Collections.Generic;
// using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;

public class AuroraData
{
    public static AuroraData Instance;
    public Game game = Game.KotOR;

    public string moduleName;
    public AuroraArchive rim, srim;
    public AuroraArchive dlg, mod;

    public StateSystem stateManager;
    public AISystem aiManager;
    public LoadingSystem loader;

    public KEYObject keyObject;
    public AuroraArchive[] bifObjects;
    public AuroraArchive textures, guiTextures;
    public Module module;

    public Dictionary<string, ERFObject> lips = new Dictionary<string, ERFObject>();
    public Dictionary<string, string> voLocations = new Dictionary<string, string>();

    public Dictionary<string, _2DAObject> loaded2das = new Dictionary<string, _2DAObject>();
    // public TLKObject tlk;
    public TLK tlk;

    public List<string> overrideFiles = new List<string>();
    public Dictionary<(string, ResourceType), string> overridePaths = new Dictionary<(string, ResourceType), string>();

    //public static Dictionary<ResourceType, string> ExtMap = new Dictionary<ResourceType, string>()
    //{
    //    { ResourceType.MDL, "mdl" },
    //    { ResourceType.UTC, "utc" },
    //    { ResourceType.UTD, "utd" },
    //    { ResourceType.UTP, "utp" },
    //    { ResourceType.UTT, "utt" },
    //    { ResourceType.UTE, "ute" },
    //    { ResourceType.UTS, "uts" },
    //    { ResourceType.UTM, "utm" },
    //    { ResourceType.UTW, "utw" },
    //    { ResourceType.CAM, "cam" },
    //};

    public static Dictionary<string, ResourceType> Ext2RTMap = new Dictionary<string, ResourceType>()
    {
        { "mdl", ResourceType.MDL },
        { "mdx", ResourceType.MDX },
        { "utc", ResourceType.UTC },
        { "utd", ResourceType.UTD },
        { "utp", ResourceType.UTP },
        { "utt", ResourceType.UTT },
        { "ute", ResourceType.UTE },
        { "uts", ResourceType.UTS },
        { "utm", ResourceType.UTM },
        { "utw", ResourceType.UTW },
        { "cam", ResourceType.CAM },
        { "lyt", ResourceType.LYT },
        { "tga", ResourceType.TGA },
        { "tpc", ResourceType.TPC },
        { "2da", ResourceType.TDA }
    };

    public AuroraData(Game game, string moduleName, bool instantiateModule = true)
    {
        AuroraData.Instance = this;
        this.game = game;
        this.moduleName = moduleName;

        stateManager = GameObject.Find("State System").GetComponent<StateSystem>();
        aiManager = GameObject.Find("AI System").GetComponent<AISystem>();
        loader = GameObject.Find("Loading System").GetComponent<LoadingSystem>();

        LoadBase();

        LoadOverride();

        if (moduleName != null)
            LoadModule(instantiateModule);
    }

    void LoadOverride()
    {
        // foreach (string path in Directory.GetFiles(AuroraPrefs.GetKotorLocation() + "\\override\\", "*", SearchOption.AllDirectories))
        foreach (string path in Directory.GetFiles(GetPath("Override/"), "*", SearchOption.AllDirectories))
        {
            string name = Path.GetFileNameWithoutExtension(path);
            string ext = Path.GetExtension(path).Replace(".", "");

            if (!Ext2RTMap.ContainsKey(ext))
            {
                continue;
            }

            ResourceType rt = Ext2RTMap[ext];
            overridePaths[(name, rt)] = path;
        }
    }

    void LoadBase()
    {
        if (AuroraPrefs.DeveloperMode())
        {
            LoadFromDirectories();
        }
        else
        {
            LoadFromGameFiles();
        }
    }

    public static string GetPath(string filename)
    {
        return Path.Join(AuroraPrefs.GetKotorLocation(), filename.Replace("\\", "/"));
    }

    void LoadFromDirectories()
    {
        loaded2das = new Dictionary<string, _2DAObject>();

        List<FolderObject> dataFolders = new List<FolderObject>();

        // We assume that the subfolders of the "data" folder
        // contain all the BIF objects
        foreach (string dir in Directory.EnumerateDirectories(GetPath("data")))
        {
            dataFolders.Add(new FolderObject(dir));
        }
        bifObjects = dataFolders.ToArray();

        textures = new FolderObject(GetPath("textures/tpa"));
        guiTextures = new FolderObject(GetPath("textures/gui"));

        // Load the VO directory
        string voicedir = GetPath("vo");

        foreach (string filepath in Directory.GetFiles(voicedir, "*.wav", SearchOption.AllDirectories))
        {
            string filename = filepath.Split('\\').Last().Replace(".wav", "");
            voLocations[filename.ToLower()] = filepath;
        }

        // TODO: Fix this
        // string tlkXML = AuroraEngine.Resources.RunXoreosTools("\"" + AuroraPrefs.GetKotorLocation() + "\\dialog.tlk\"", "tlk2xml", AuroraPrefs.TargetGame() == Game.KotOR ? "--kotor" : "--kotor2");
        // tlk = new TLKObject(tlkXML);

        UnityEngine.Debug.Log("Loaded " + tlk.strings.Count + " strings from the TLK");
    }

    void LoadFromGameFiles()
    {
        loaded2das = new Dictionary<string, _2DAObject>();

        // keyObject = new KEYObject(AuroraPrefs.GetKotorLocation() + "\\chitin.key");
        keyObject = new KEYObject(GetPath("chitin.key"));
        Debug.Log("Loaded keyObject: " + keyObject);

        KEYObject.BIFStream[] bifs = keyObject.GetBIFs();
        bifObjects = new BIFObject[bifs.Length];
        for (int i = 0; i < bifs.Length; i++)
        {
            bifObjects[i] = new BIFObject(GetPath(bifs[i].Filename), keyObject);
        }

        textures = new ERFObject(GetPath("TexturePacks/swpc_tex_tpa.erf"));
        guiTextures = new ERFObject(GetPath("TexturePacks/swpc_tex_gui.erf"));

        // Load the VO directory
        string voicedir;
        if (AuroraPrefs.TargetGame() == Game.KotOR)
        {
            voicedir = GetPath("streamwaves");
        }
        else
        {
            voicedir = GetPath("streamvoice");
        }

        foreach (string filepath in Directory.GetFiles(voicedir, "*.wav", SearchOption.AllDirectories))
        {
            string filename = filepath.Split('\\').Last().Replace(".wav", "");
            voLocations[filename.ToLower()] = filepath;
        }

        // Stream tlk_stream = new FileStream(AuroraPrefs.GetKotorLocation() + "\\dialog.tlk", FileMode.Open);
        // int b = tlk_stream.ReadByte();
        // UnityEngine.Debug.Log(b);
        // tlk_stream.Seek(0, SeekOrigin.Begin);
        // tlk = new TLK();
        // tlk.Load(tlk_stream, new Dictionary<string, Stream>(), 0, 0);

        // string tlkXML = AuroraEngine.Resources.RunXoreosTools("\"" + AuroraPrefs.GetKotorLocation() + "\\dialog.tlk\"", "tlk2xml", AuroraPrefs.TargetGame() == Game.KotOR ? "--kotor" : "--kotor2");
        // tlk = new TLKObject(tlkXML);

        tlk = AuroraEngine.Resources.LoadTLK();

        // UnityEngine.Debug.Log("Loaded " + tlk.strings.Count + " strings from the TLK");
    }

    void LoadModule(bool instantiateModule)
    {
        if (AuroraPrefs.DeveloperMode())
        {
            LoadModuleFromFolders(instantiateModule);
        }
        else
        {
            LoadModuleFromGameFiles(instantiateModule);
        }
    }

    void LoadModuleFromFolders(bool instantiateModule)
    {
        rim = null;
        srim = null;
        dlg = null;

        // mod = new FolderObject(AuroraPrefs.GetKotorLocation() + "\\modules\\" + moduleName);
        mod = new FolderObject(GetPath("modules/" + moduleName));
        module = new Module(moduleName, this, instantiateModule);
    }

    public void LoadModuleFromGameFiles(bool instantiateModule)
    {
        rim = null;
        srim = null;
        dlg = null;
        mod = null;

        string rimPath = GetPath("modules/" + moduleName + ".rim");
        string srimPath = GetPath("modules/" + moduleName + "_s.rim");
        string dlgPath = GetPath("modules/" + moduleName + ".dlg");
        string modPath = GetPath("modules/" + moduleName + ".mod");

        if (File.Exists(rimPath))
        {
            rim = new RIMObject(rimPath);
            //UnityEngine.Debug.Log("Loaded " + rim.resources.Keys.Count + " items from " + moduleName + ".rim");
        }

        if (File.Exists(srimPath))
        {
            srim = new RIMObject(srimPath);
            //UnityEngine.Debug.Log("Loaded " + srim.resources.Keys.Count + " items from " + moduleName + "_s.rim");
        }

        if (File.Exists(dlgPath))
        {
            dlg = new ERFObject(dlgPath);
            //UnityEngine.Debug.Log("Loaded " + dlg.resourceKeys.Count + " items from " + moduleName + "_dlg.erf");
        }

        if (File.Exists(modPath))
        {
            mod = new ERFObject(modPath);
            //UnityEngine.Debug.Log("Loaded " + mod.resourceKeys.Count + " items from " + moduleName + ".mod");
        }

        if (rim == null && srim == null && dlg == null && mod == null)
        {
            UnityEngine.Debug.LogError("Could not find any files for module " + moduleName);
        }

        module = new Module(moduleName, this, instantiateModule);
    }

    public string From2DA(string name, int idx, string row)
    {
        if (!loaded2das.ContainsKey(name))
        {
            Load2DA(name);
        }
        _2DAObject _2da = loaded2das[name];
        return _2da[idx, row];
    }

    public _2DAObject Load2DA(string resref)
    {
        _2DAObject _2da;

        if (loaded2das.TryGetValue(resref, out _2da))
        {
            return _2da;
        }

        Stream stream = GetStream(resref, ResourceType.TDA);
        if (stream == null)
        {
            UnityEngine.Debug.LogWarning("Missing 2da: " + resref);
            return null;
        }
        else
        {
            _2da = new _2DAObject(stream);
            loaded2das.Add(resref, _2da);
            return _2da;
        }
    }


    public T Get<T>(string resref, ResourceType rt)
    {
        // Check the override folder
        T obj = GetFromOverride<T>(resref, rt);
        if (obj != null)
        {
            return obj;
        }

        // Check the module
        obj = GetFromModule<T>(resref, rt);
        if (obj != null)
            return obj;

        // Check the base game files
        obj = GetFromBase<T>(resref, rt);
        if (obj != null)
            return obj;

        return default;
    }

    T GetFromOverride<T>(string resref, ResourceType rt)
    {
        Stream stream = GetStreamFromOverride(resref, rt);
        if (stream == null)
            return default;

        GFFObject obj = new GFFLoader(stream).GetObject();
        stream.Close();
        return (T)obj.Serialize<T>();
    }

    Stream GetStreamFromOverride(string resref, ResourceType rt)
    {
        if (overridePaths.ContainsKey((resref, rt)))
        {
            return new FileStream(overridePaths[(resref, rt)], FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }
        return null;
    }

    public Stream GetStream(string resref, ResourceType rt)
    {
        resref = resref.ToLower();
        Stream stream = GetStreamFromOverride(resref, rt);
        if (stream != null)
            return stream;

        stream = GetStreamFromModule(resref, rt);
        if (stream != null)
            return stream;

        return GetStreamFromBase(resref, rt);
    }

    Stream GetStreamFromModule(string resref, ResourceType rt, bool warnOnFail = true)
    {
        // Check if the item is in the rim

        Stream stream = null;

        if (stream == null && mod != null)
        {
            stream = mod.GetResource(resref, rt);
        }
        if (stream == null && dlg != null)
        {
            stream = dlg.GetResource(resref, rt);
        }
        if (stream == null && srim != null)
        {
            stream = srim.GetResource(resref, rt);
        }
        if (stream == null && rim != null)
        {
            stream = rim.GetResource(resref, rt);
        }

        if (warnOnFail && stream == null)
        {
            UnityEngine.Debug.LogWarning("Failed to load file " + resref + " of type " + rt);
        }

        return stream;
    }

    T GetFromModule<T>(string resref, ResourceType rt)
    {
        Stream stream = GetStreamFromModule(resref, rt);

        if (stream == null)
            return default;

        GFFObject obj = new GFFLoader(stream).GetObject();

        return (T)obj.Serialize<T>();
    }

    T GetFromBase<T>(string resref, ResourceType rt)
    {
        Stream stream = GetStreamFromBase(resref, rt);

        if (stream == null)
            return default;

        GFFObject gff = new GFFLoader(stream).GetObject();
        return (T)gff.Serialize<T>();
    }

    Stream GetStreamFromBase(string resref, ResourceType type)
    {
        Stream resourceStream;

        // Try to load from BIFObjects
        foreach (AuroraArchive bifObject in bifObjects)
        {
            if ((resourceStream = bifObject.GetResource(resref, type)) != null)
            {
                return resourceStream;
            }
        }

        // Try to load from lip ERFs
        foreach (AuroraArchive erf in lips.Values)
        {
            if ((resourceStream = erf.GetResource(resref, ResourceType.LIP)) != null)
            {
                return resourceStream;
            }
        }

        // And finally, try and load from global ERFs
        if ((resourceStream = textures.GetResource(resref, type)) != null)
        {
            return resourceStream;
        }
        if ((resourceStream = guiTextures.GetResource(resref, type)) != null)
        {
            return resourceStream;
        }

        return resourceStream;
    }

    public HashSet<(string, ResourceType)> ModuleResources()
    {
        HashSet<(string, ResourceType)> resources = new HashSet<(string, ResourceType)>();

        if (AuroraPrefs.DeveloperMode())
        {
            foreach ((string resref, ResourceType rt) in ((FolderObject)mod).resources.Keys)
            {
                resources.Add((resref, rt));
            }
            return resources;
        }

        // List items from .rim
        if (rim != null)
        {
            foreach ((string resref, ResourceType rt) in ((RIMObject)rim).resources.Keys)
            {
                resources.Add((resref, rt));
            }
        }

        // List items from _s.rim
        if (srim != null)
        {
            foreach ((string resref, ResourceType rt) in ((RIMObject)srim).resources.Keys)
            {
                resources.Add((resref, rt));
            }
        }

        // List items from _dlg.erf
        if (dlg != null)
        {
            foreach ((string resref, ResourceType rt) in ((ERFObject)dlg).resourceKeys.Keys)
            {
                resources.Add((resref, rt));
            }
        }

        // List items from .mod
        if (mod != null)
        {
            foreach ((string resref, ResourceType rt) in ((ERFObject)mod).resourceKeys.Keys)
            {
                resources.Add((resref, rt));
            }
        }
        return resources;
    }
}