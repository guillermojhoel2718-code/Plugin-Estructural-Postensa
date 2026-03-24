using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Windows.Forms;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

using WinColor  = System.Drawing.Color;
using WinPoint  = System.Drawing.Point;
using WinSize   = System.Drawing.Size;
using WinLabel  = System.Windows.Forms.Label;
using WinPanel  = System.Windows.Forms.Panel;
using WinButton = System.Windows.Forms.Button;
using RevColor  = Autodesk.Revit.DB.Color;
using RevView   = Autodesk.Revit.DB.View;
using TaskDlg   = Autodesk.Revit.UI.TaskDialog;

namespace PluginEstructural
{
    public partial class ThisApplication
    {
        private void Module_Startup(object sender, EventArgs e) { }
        private void Module_Shutdown(object sender, EventArgs e) { }
    }

    // ════════════════════════════════════════════════════════
    //  CONFIG
    // ════════════════════════════════════════════════════════
    internal static class Config
    {
        static Dictionary<string, string> _v;
        internal static string Get(string key)
        {
            if (_v == null)
            {
                _v = new Dictionary<string, string>();
                string[] rutas = {
                    @"C:\ProgramData\Autodesk\Revit\Addins\2025\config.env",
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.env"),
                    Path.Combine(Environment.GetFolderPath(
                        Environment.SpecialFolder.MyDocuments), "PluginEstructural", "config.env")
                };
                foreach (var r in rutas)
                {
                    if (!File.Exists(r)) continue;
                    foreach (var l in File.ReadAllLines(r))
                    {
                        if (l.StartsWith("#") || !l.Contains("=")) continue;
                        var p = l.Split(new[] { '=' }, 2);
                        if (p.Length == 2) _v[p[0].Trim()] = p[1].Trim();
                    }
                    break;
                }
            }
            return _v.ContainsKey(key) ? _v[key] : null;
        }
    }

    // ════════════════════════════════════════════════════════
    //  ESTILOS UI
    // ════════════════════════════════════════════════════════
    internal static class Esti
    {
        internal static WinColor FondoOscuro     => WinColor.FromArgb(240, 242, 245);
        internal static WinColor FondoMedio      => WinColor.FromArgb(225, 230, 235);
        internal static WinColor FondoPanel      => WinColor.FromArgb(255, 255, 255);
        internal static WinColor Acento          => WinColor.FromArgb(22,  160, 230);
        internal static WinColor TextoPrincipal  => WinColor.FromArgb(40,  50,  60);
        internal static WinColor TextoSecundario => WinColor.FromArgb(100, 110, 120);
        internal static WinColor ColVerde        => WinColor.FromArgb(39,  201, 111);
        internal static WinColor ColAmarillo     => WinColor.FromArgb(255, 196, 0);
        internal static WinColor ColRojo         => WinColor.FromArgb(229, 57,  53);
        internal static WinColor ColGris         => WinColor.FromArgb(200, 210, 220);

        internal static Font FTitulo => new Font("Segoe UI", 10f, FontStyle.Bold);
        internal static Font FNormal => new Font("Segoe UI",  9f, FontStyle.Regular);
        internal static Font FPeque  => new Font("Segoe UI",  8f, FontStyle.Regular);
        internal static Font FBold   => new Font("Segoe UI",  9f, FontStyle.Bold);

        internal static WinButton Btn(string txt, WinColor bg, int x, int y, int w = 120, int h = 32)
        {
            var b = new WinButton
            {
                Text = txt, Location = new WinPoint(x, y), Size = new WinSize(w, h),
                BackColor = bg, ForeColor = (bg == Acento || bg == ColGris || bg == ColRojo) ? WinColor.White : TextoPrincipal, FlatStyle = FlatStyle.Flat,
                Font = FBold, Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = (bg == Acento) ? WinColor.FromArgb(41, 182, 246) : FondoMedio;
            return b;
        }

        internal static WinButton BtnGrande(string txt, int y, bool activo = true, WinColor? bg = null)
        {
            var fondo = activo ? (bg ?? FondoPanel) : FondoMedio;
            var b = new WinButton
            {
                Text = txt, Location = new WinPoint(16, y), Size = new WinSize(434, 54),
                BackColor = fondo, ForeColor = activo ? TextoPrincipal : TextoSecundario,
                FlatStyle = FlatStyle.Flat, Font = FNormal,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(14, 0, 0, 0),
                Cursor = activo ? Cursors.Hand : Cursors.Default,
                Enabled = activo
            };
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = activo ? Acento : ColGris;
            b.FlatAppearance.MouseOverBackColor = activo ? FondoMedio : FondoMedio;
            return b;
        }

        internal static WinLabel Titulo(string txt, int x, int y) =>
            new WinLabel { Text = txt, Location = new WinPoint(x, y), AutoSize = true,
                Font = FTitulo, ForeColor = Acento, BackColor = WinColor.Transparent };

        internal static WinLabel Lbl(string txt, int x, int y, WinColor? col = null) =>
            new WinLabel { Text = txt, Location = new WinPoint(x, y), AutoSize = true,
                Font = FNormal, ForeColor = col ?? TextoSecundario, BackColor = WinColor.Transparent };

        internal static WinPanel PanelInfo(string txt, int y, WinColor borde, int h = 44)
        {
            var p = new WinPanel { Location = new WinPoint(16, y), Size = new WinSize(434, h),
                BackColor = FondoPanel };
            var l = new WinLabel { Text = txt, Location = new WinPoint(14, 7),
                Size = new WinSize(410, h - 14), Font = FPeque,
                ForeColor = TextoSecundario, BackColor = WinColor.Transparent };
            p.Controls.Add(l);
            var bc = borde;
            p.Paint += (s, e) => e.Graphics.FillRectangle(new SolidBrush(bc), 0, 0, 3, h);
            return p;
        }

        internal static WinPanel Sep(int y) =>
            new WinPanel { Location = new WinPoint(16, y), Size = new WinSize(434, 1),
                BackColor = ColGris };

        internal static WinPanel HeaderLogo(int ancho)
        {
            var header = new WinPanel
            {
                Location = new WinPoint(0, 0), Size = new WinSize(ancho, 64),
                BackColor = WinColor.White
            };
            
            System.Windows.Forms.Control logoCtrl;
            string logoPath = @"C:\ProgramData\Autodesk\Revit\Macros\2025\Revit\AppHookup\PluginEstructural\Source\PluginEstructural\Desing\Logo postensa.png";
            if (System.IO.File.Exists(logoPath))
            {
                logoCtrl = new System.Windows.Forms.PictureBox
                {
                    Image = Image.FromFile(logoPath),
                    SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom,
                    Location = new WinPoint(16, 8), Size = new WinSize(140, 48),
                    BackColor = WinColor.Transparent
                };
            }
            else
            {
                logoCtrl = new WinLabel
                {
                    Text = "POSTENSA",
                    Location = new WinPoint(16, 8), AutoSize = false, Size = new WinSize(160, 48),
                    Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                    ForeColor = Acento, BackColor = WinColor.Transparent,
                    TextAlign = ContentAlignment.MiddleLeft
                };
            }
            
            var sub = new WinLabel
            {
                Text = "Design & Build — Plugin BIM Estructural",
                Location = new WinPoint(180, 22), AutoSize = true,
                Font = new Font("Segoe UI", 8f, FontStyle.Regular),
                ForeColor = TextoSecundario, BackColor = WinColor.Transparent
            };
            var ver = new WinLabel
            {
                Text = "v2.0", Location = new WinPoint(ancho - 60, 24), AutoSize = true,
                Font = new Font("Segoe UI", 8f, FontStyle.Regular),
                ForeColor = TextoPrincipal, BackColor = WinColor.Transparent
            };
            header.Paint += (s, e) =>
                e.Graphics.FillRectangle(new SolidBrush(Acento), 0, 61, ancho, 3);
            header.Controls.AddRange(new System.Windows.Forms.Control[] { logoCtrl, sub, ver });
            return header;
        }
    }

    // ════════════════════════════════════════════════════════
    //  UTILIDADES
    // ════════════════════════════════════════════════════════
    internal static class Utils
    {
        internal static readonly List<BuiltInCategory> Cats = new List<BuiltInCategory>
        {
            BuiltInCategory.OST_StructuralFraming,    BuiltInCategory.OST_StructuralColumns,
            BuiltInCategory.OST_StructuralFoundation, BuiltInCategory.OST_Floors,
            BuiltInCategory.OST_Walls,                BuiltInCategory.OST_Roofs,
            BuiltInCategory.OST_Parts,                BuiltInCategory.OST_Ramps,
            BuiltInCategory.OST_Stairs,               BuiltInCategory.OST_StructuralTruss,
            BuiltInCategory.OST_StructuralStiffener,  BuiltInCategory.OST_StructConnections,
            BuiltInCategory.OST_GenericModel,         BuiltInCategory.OST_StairsRailing,
        };

        internal static readonly HashSet<long> CatsAcero = new HashSet<long>
        {
            (long)BuiltInCategory.OST_StructuralFraming,
            (long)BuiltInCategory.OST_StructuralTruss,
            (long)BuiltInCategory.OST_StructConnections,
            (long)BuiltInCategory.OST_StructuralStiffener,
        };

        internal static bool EsPermitido(Element e) =>
            e?.Category != null && Cats.Any(c => (long)c == e.Category.Id.Value);

        internal static List<Solid> ObtenerSolidos(Element elem)
        {
            var lst = new List<Solid>();
            bool ac = elem.Category != null && CatsAcero.Contains(elem.Category.Id.Value);
            ProcGeo(elem.get_Geometry(new Options
                { DetailLevel = ViewDetailLevel.Fine, ComputeReferences = true }), lst, ac);
            return lst;
        }

        static void ProcGeo(GeometryElement g, List<Solid> lst, bool ac)
        {
            if (g == null) return;
            foreach (GeometryObject o in g)
            {
                if (o is Solid s && s.Volume > 0.0001 && s.Faces.Size > 0)
                { if (ac || s.Volume / s.SurfaceArea > 0.001) lst.Add(s); }
                else if (o is GeometryInstance gi) ProcGeo(gi.GetInstanceGeometry(), lst, ac);
            }
        }

        internal static string RutaRegistro(Document doc)
        {
            string b = string.IsNullOrEmpty(doc.PathName)
                ? Path.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.MyDocuments), "PluginEstructural")
                : Path.GetDirectoryName(doc.PathName);
            return Path.Combine(b, "Registro_BIM");
        }

        internal static string Safe(string s)
        {
            foreach (char c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return s;
        }

        internal static RevColor Verde    => new RevColor(39,  201, 111);
        internal static RevColor Amarillo => new RevColor(255, 196, 0);

        internal static void ExportarIFC(Document doc, string carpeta, string nombre)
        {
            Directory.CreateDirectory(carpeta);
            var opts = new IFCExportOptions
                { FileVersion = IFCVersion.IFC2x3CV2, ExportBaseQuantities = true };
            // NOTA: doc.Export para IFC gestiona su propia transaccion internamente;
            // NO envolver con Transaction externa (causaria conflicto con ActiveView).
            doc.Export(carpeta, nombre, opts);
        }

        internal static void OcultarCategoriasRuido(Document doc, View3D vista)
        {
            var builtIn = new[]
            {
                BuiltInCategory.OST_Dimensions,      BuiltInCategory.OST_TextNotes,
                BuiltInCategory.OST_Grids,           BuiltInCategory.OST_Levels,
                BuiltInCategory.OST_CLines, BuiltInCategory.OST_RasterImages,
                BuiltInCategory.OST_Mass,            BuiltInCategory.OST_Topography,
                BuiltInCategory.OST_Cameras,         BuiltInCategory.OST_SectionBox,
                BuiltInCategory.OST_SketchLines,
            };
            foreach (var c in builtIn)
            {
                try
                {
                    var cat = Category.GetCategory(doc, c);
                    if (cat != null && vista.CanCategoryBeHidden(cat.Id))
                        vista.SetCategoryHidden(cat.Id, true);
                }
                catch { }
            }
            foreach (Category cat in doc.Settings.Categories)
            {
                try
                {
                    if (cat.Name.Contains(".dwg") || cat.Name.Contains(".dxf") ||
                        cat.Name.Contains(".dgn") || cat.Name.Contains(".sat") ||
                        cat.Name.Contains("Import") || cat.Name.Contains("Linked") ||
                        cat.Name.Contains(".rvt"))
                        if (vista.CanCategoryBeHidden(cat.Id))
                            vista.SetCategoryHidden(cat.Id, true);
                }
                catch { }
            }
        }
    }

    // ════════════════════════════════════════════════════════
    //  HASH
    // ════════════════════════════════════════════════════════
    internal static class HashElem
    {
        internal static string Calcular(Element e)
        {
            var sb = new StringBuilder();
            sb.Append(e.Category?.Name ?? "");
            var bb = e.get_BoundingBox(null);
            if (bb != null)
            {
                sb.Append(bb.Min.X.ToString("F3")); sb.Append(bb.Min.Y.ToString("F3"));
                sb.Append(bb.Min.Z.ToString("F3")); sb.Append(bb.Max.X.ToString("F3"));
                sb.Append(bb.Max.Y.ToString("F3")); sb.Append(bb.Max.Z.ToString("F3"));
            }
            var parametros = e.Parameters.Cast<Parameter>().OrderBy(p => p.Definition.Name);
            foreach (Parameter p in parametros)
                try { if (p.HasValue && !p.IsReadOnly) sb.Append(p.AsValueString() ?? ""); } catch { }

            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(sb.ToString());
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "");
            }
        }

        internal static Dictionary<string, string> ParamsLegibles(Element e)
        {
            var dic = new Dictionary<string, string>();
            var nombres = new[] { "b", "h", "Ancho", "Alto", "Espesor", "Longitud",
                "Desfase base", "Desfase superior", "Tipo", "Marca de ubicación", "Marca" };
            foreach (var n in nombres)
            {
                var p = e.LookupParameter(n);
                if (p != null && p.HasValue)
                    dic[n] = p.AsValueString() ?? p.AsString() ?? "";
            }
            var bb = e.get_BoundingBox(null);
            if (bb != null) dic["Pos Z"] = bb.Min.Z.ToString("F2") + "m";

            var pCol = e.get_Parameter(BuiltInParameter.COLUMN_LOCATION_MARK);
            if (pCol != null && pCol.HasValue) dic["Marca de Ubicacion"] = pCol.AsValueString() ?? pCol.AsString();
            else if (dic.ContainsKey("Marca de ubicación") && !string.IsNullOrEmpty(dic["Marca de ubicación"])) dic["Marca de Ubicacion"] = dic["Marca de ubicación"];
            else if (dic.ContainsKey("Marca") && !string.IsNullOrEmpty(dic["Marca"])) dic["Marca de Ubicacion"] = dic["Marca"];
            else dic["Marca de Ubicacion"] = "-";

            var paramFto = e.get_Parameter(BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM);
            dic["Familia y Tipo"] = paramFto != null && paramFto.AsValueString() != null
                ? paramFto.AsValueString() : (e.Category?.Name + " - " + e.Name);

            // ── Deteccion de nivel ampliada ─────────────────────────────────
            // 1. Recorrer lista de parametros built-in de nivel
            string nivel = null;
            var bipNiveles = new[]
            {
                BuiltInParameter.FAMILY_LEVEL_PARAM,
                BuiltInParameter.SCHEDULE_LEVEL_PARAM,
                BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM,
                BuiltInParameter.FAMILY_BASE_LEVEL_PARAM,
                BuiltInParameter.WALL_BASE_CONSTRAINT,
                BuiltInParameter.ROOF_CONSTRAINT_LEVEL_PARAM,
                BuiltInParameter.STAIRS_BASE_LEVEL_PARAM,
                BuiltInParameter.INSTANCE_SCHEDULE_ONLY_LEVEL_PARAM,
                BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM,
            };
            foreach (var bip in bipNiveles)
            {
                try
                {
                    var p = e.get_Parameter(bip);
                    if (p == null || p.StorageType != StorageType.ElementId) continue;
                    var eid = p.AsElementId();
                    if (eid == ElementId.InvalidElementId) continue;
                    var lvl = e.Document.GetElement(eid) as Level;
                    if (lvl != null) { nivel = lvl.Name; break; }
                }
                catch { }
            }

            // 2. Plano de trabajo (SketchPlane)
            if (nivel == null)
            {
                try
                {
                    var p = e.get_Parameter(BuiltInParameter.SKETCH_PLANE_PARAM);
                    if (p != null && p.StorageType == StorageType.ElementId)
                    {
                        var sp = e.Document.GetElement(p.AsElementId()) as SketchPlane;
                        if (sp != null) nivel = "Plano: " + sp.Name;
                    }
                }
                catch { }
            }

            // 3. Elemento anfitrion (FamilyInstance hosted)
            if (nivel == null && e is FamilyInstance fi && fi.Host != null)
            {
                try
                {
                    foreach (var bip in new[] { BuiltInParameter.FAMILY_LEVEL_PARAM, BuiltInParameter.SCHEDULE_LEVEL_PARAM })
                    {
                        var p = fi.Host.get_Parameter(bip);
                        if (p == null || p.StorageType != StorageType.ElementId) continue;
                        var lvl = e.Document.GetElement(p.AsElementId()) as Level;
                        if (lvl != null) { nivel = lvl.Name; break; }
                    }
                }
                catch { }
            }

            // 4. Fallback: nivel mas cercano por Z del bounding box
            if (nivel == null && bb != null)
            {
                try
                {
                    double z = bb.Min.Z;
                    var nearest = new FilteredElementCollector(e.Document)
                        .OfClass(typeof(Level)).Cast<Level>()
                        .Where(l => l.Elevation <= z + 0.5)
                        .OrderByDescending(l => l.Elevation)
                        .FirstOrDefault();
                    if (nearest != null) nivel = "~" + nearest.Name;
                }
                catch { }
            }

            dic["Nivel / Restriccion"] = nivel ?? "Sin nivel";
            return dic;
        }

    }

    // ════════════════════════════════════════════════════════
    //  GEMINI
    // ════════════════════════════════════════════════════════
    internal static class Gemini
    {
        static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };

        internal static string Describir(string cat, string tipo,
            Dictionary<string, string> antes, Dictionary<string, string> despues,
            string nivel = null, string famTipo = null)
        {
            string fallback = Auto(tipo, antes, despues, famTipo);
            string key = Config.Get("GEMINI_API_KEY");
            if (string.IsNullOrEmpty(key)) return fallback;
            try
            {
                var cambiosList = new List<string>();
                foreach (var k in antes.Keys)
                    if (despues.ContainsKey(k) && antes[k] != despues[k])
                        cambiosList.Add(k + ": [" + antes[k] + "] → [" + despues[k] + "]");
                if (cambiosList.Count == 0) return fallback;

                // Dimensiones nuevas relevantes
                var dims = new List<string>();
                foreach (var k in new[] { "b", "h", "Ancho", "Alto", "Espesor", "Longitud" })
                    if (despues.ContainsKey(k)) dims.Add(k + "=" + despues[k]);

                string prompt =
                    "Eres BIM Manager de estructuras en POSTENSA Design & Build. " +
                    "Redacta UNA descripcion tecnica en español (MAXIMO 130 caracteres, SIN comillas, SIN punto final). " +
                    "Se especifico sobre el cambio: menciona valores numericos si los hay, y la implicacion estructural. " +
                    "Elemento: " + (famTipo ?? tipo) + " | Categoria: " + cat +
                    (nivel != null ? " | Nivel: " + nivel : "") +
                    (dims.Count > 0 ? " | Dimensiones actuales: " + string.Join(", ", dims) : "") +
                    " | Cambios detectados: " + string.Join(" / ", cambiosList) + ".";

                string json = "{\"contents\":[{\"parts\":[{\"text\":\"" +
                    prompt.Replace("\\", "").Replace("\"", "'").Replace("\n", " ") + "\"}]}]," +
                    "\"generationConfig\":{\"maxOutputTokens\":60,\"temperature\":0.2}}";

                string url = "https://generativelanguage.googleapis.com/v1beta/models/" +
                    "gemini-1.5-flash:generateContent?key=" + key;

                var t = _http.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
                t.Wait(15000);
                if (!t.Result.IsSuccessStatusCode) return fallback;
                string resp = t.Result.Content.ReadAsStringAsync().Result;
                int i = resp.IndexOf("\"text\":"); if (i < 0) return fallback;
                i = resp.IndexOf("\"", i + 7) + 1;
                int f = resp.IndexOf("\"", i);
                if (f <= i) return fallback;
                string res = resp.Substring(i, f - i).Trim().Replace("\\n", " ").Replace("\\u003e", ">");
                return string.IsNullOrEmpty(res) ? fallback : res;
            }
            catch { return fallback; }
        }

        internal static string NuevoDesc(string cat, string famTipo)
            => "Nuevo elemento: " + famTipo + " (" + cat + ")";

        internal static string ElimDesc(string cat, string famTipo)
            => "Eliminado: " + famTipo + " (" + cat + ")";

        static string Auto(string tipo, Dictionary<string, string> a, Dictionary<string, string> d, string famTipo)
        {
            var cambios = a.Keys.Where(k => d.ContainsKey(k) && a[k] != d[k])
                .Select(k => k + ": " + a[k] + " > " + d[k]).ToList();
            string nombre = famTipo ?? tipo;
            return cambios.Count == 0
                ? nombre + ": cambio en geometria o posicion"
                : nombre + " — " + string.Join(", ", cambios.Take(3));
        }

        internal static string AutoDesc(string cat, string tipo,
            Dictionary<string, string> antes, Dictionary<string, string> despues, string famTipo)
            => Auto(tipo, antes, despues, famTipo);
    }

    // ════════════════════════════════════════════════════════
    //  RIBBON
    // ════════════════════════════════════════════════════════
    public class AppInicio : IExternalApplication
    {
        private System.Reflection.Assembly ResolutorLibrerias(object sender, ResolveEventArgs args)
        {
            try
            {
                var asmName = new System.Reflection.AssemblyName(args.Name);
                string dllPath = Path.Combine(Path.GetDirectoryName(typeof(AppInicio).Assembly.Location), asmName.Name + ".dll");
                if (File.Exists(dllPath)) return System.Reflection.Assembly.LoadFrom(dllPath);
            }
            catch { }
            return null;
        }

        public Result OnStartup(UIControlledApplication app)
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolutorLibrerias;
            try
            {
                const string TAB = "BIM Estructural";
                try { app.CreateRibbonTab(TAB); } catch { }
                var panel = app.CreateRibbonPanel(TAB, "Postensa BIM");
                string dll = typeof(AppInicio).Assembly.Location;
                panel.AddItem(new PushButtonData("SAT", "Exportar\nSAT", dll,
                    "PluginEstructural.CmdExportarSAT")
                    { ToolTip = "Genera solido unico -90 para SolidWorks" });
                panel.AddSeparator();
                panel.AddItem(new PushButtonData("Comparar", "Comparar\nVersiones", dll,
                    "PluginEstructural.CmdCompararVersiones")
                    { ToolTip = "OLD/NEW con reporte IA y vista limpia" });
                panel.AddSeparator();
                panel.AddItem(new PushButtonData("Corte", "Corte\nGeometrico", dll,
                    "PluginEstructural.CmdCorteJerarquico")
                    { ToolTip = "Une y corta geometria estructural de forma jerarquica" });
                panel.AddSeparator();
                panel.AddItem(new PushButtonData("SharedView", "Vista\nCompartida", dll,
                    "PluginEstructural.CmdCompartirVista")
                    { ToolTip = "Abre Vistas Compartidas de Revit para compartir el modelo online" });
                panel.AddSeparator();
                panel.AddItem(new PushButtonData("Casetones", "Colocar\nCasetones", dll,
                    "PluginEstructural.CmdColocarCasetones")
                    { ToolTip = "Coloca familias desde reticula CAD sobre la losa" });
                panel.AddSeparator();
                panel.AddItem(new PushButtonData("Renombrar", "Renombrar\nFamilias", dll,
                    "PluginEstructural.CmdRenombrarFamilias")
                    { ToolTip = "Renombra familias filtradas agregando prefijos, sufijos y reemplazos de texto" });
                return Result.Succeeded;
            }
            catch (Exception ex) { TaskDlg.Show("Error", ex.Message); return Result.Failed; }
        }
        public Result OnShutdown(UIControlledApplication app) => Result.Succeeded;
    }


    // ════════════════════════════════════════════════════════
    //  CMD 1 — EXPORTAR SAT
    // ════════════════════════════════════════════════════════
    [Transaction(TransactionMode.Manual)]
    public class CmdExportarSAT : IExternalCommand
    {
        public Result Execute(ExternalCommandData data, ref string msg, ElementSet es)
        {
            var uidoc = data.Application.ActiveUIDocument;
            var doc   = uidoc.Document;
            using (var dlg = new VentanaSAT(doc))
            {
                if (dlg.ShowDialog() != DialogResult.OK) return Result.Cancelled;
                var ids = dlg.UsarSeleccion
                    ? uidoc.Selection.GetElementIds() : IdsModelo(doc, dlg.Niveles);
                if (ids.Count == 0)
                { TaskDlg.Show("Sin elementos", "No hay elementos estructurales."); return Result.Cancelled; }
                var sols = new List<Solid>();
                foreach (var id in ids)
                { var e = doc.GetElement(id); if (Utils.EsPermitido(e)) sols.AddRange(Utils.ObtenerSolidos(e)); }
                if (sols.Count == 0)
                { TaskDlg.Show("Error", "Sin geometria valida."); return Result.Failed; }
                var gen = new List<ElementId>();
                using (var t = new Transaction(doc, "SAT"))
                {
                    t.Start();
                    for (int i = 0; i < sols.Count; i += 100)
                        Lote(doc, sols.Skip(i).Take(100).ToList(), gen, i / 100 + 1);
                    if (gen.Count > 0) CrearVista(doc, gen);
                    t.Commit();
                }
                var vista = new FilteredElementCollector(doc).OfClass(typeof(View3D))
                    .Cast<View3D>().FirstOrDefault(v => v.Name == "EXPORTACION_SAT");
                if (vista != null) 
                {
                    uidoc.ActiveView = vista;
                    try
                    {
                        Directory.CreateDirectory(dlg.RutaExportacion);
                        var opts = new SATExportOptions();
                        doc.Export(dlg.RutaExportacion, dlg.NombreArchivo, new List<ElementId> { vista.Id }, opts);
                        TaskDlg.Show("SAT Listo",
                            "Solidos: " + sols.Count + " | Shapes: " + gen.Count +
                            "\n\nSe ha exportado el archivo SAT en:\n" + Path.Combine(dlg.RutaExportacion, dlg.NombreArchivo + ".sat") +
                            "\n\nVista EXPORTACION_SAT creada y activa.");
                    }
                    catch (Exception ex)
                    {
                        TaskDlg.Show("SAT Listo",
                            "Solidos: " + sols.Count + " | Shapes: " + gen.Count +
                            "\n\nVista EXPORTACION_SAT activa.\nNo se pudo auto-exportar (Error: " + ex.Message + ").\nExporta manualmente: Archivo > Exportar > CAD > SAT");
                    }
                }
            }
            return Result.Succeeded;
        }

        ICollection<ElementId> IdsModelo(Document doc, List<string> niveles)
        {
            var ids = new List<ElementId>();
            foreach (Element e in new FilteredElementCollector(doc).WhereElementIsNotElementType())
            {
                if (!Utils.EsPermitido(e)) continue;
                if (niveles.Count > 0)
                {
                    var lp = e.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM)
                          ?? e.get_Parameter(BuiltInParameter.SCHEDULE_LEVEL_PARAM);
                    if (lp != null)
                    {
                        var lv = doc.GetElement(lp.AsElementId()) as Level;
                        if (lv == null || !niveles.Contains(lv.Name)) continue;
                    }
                }
                ids.Add(e.Id);
            }
            return ids;
        }

        void Lote(Document doc, List<Solid> chunk, List<ElementId> gen, int idx)
        {
            IList<GeometryObject> sh = chunk.Cast<GeometryObject>().ToList();
            try
            {
                var ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
                if (ds.IsValidShape(sh))
                {
                    ds.ApplicationId = "PluginEstructural"; ds.Name = "SAT_" + idx;
                    ds.SetShape(sh);
                    ElementTransformUtils.RotateElement(doc, ds.Id,
                        Line.CreateBound(XYZ.Zero, XYZ.BasisX), -Math.PI / 2.0);
                    gen.Add(ds.Id);
                }
                else { doc.Delete(ds.Id); foreach (var s in chunk) Solo(doc, s, gen); }
            }
            catch { foreach (var s in chunk) Solo(doc, s, gen); }
        }

        void Solo(Document doc, Solid s, List<ElementId> gen)
        {
            try
            {
                IList<GeometryObject> sh = new List<GeometryObject> { s };
                var ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
                if (ds.IsValidShape(sh))
                {
                    ds.ApplicationId = "PluginEstructural"; ds.SetShape(sh);
                    ElementTransformUtils.RotateElement(doc, ds.Id,
                        Line.CreateBound(XYZ.Zero, XYZ.BasisX), -Math.PI / 2.0);
                    gen.Add(ds.Id);
                }
                else doc.Delete(ds.Id);
            }
            catch { }
        }

        void CrearVista(Document doc, List<ElementId> gen)
        {
            var vft = new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>().FirstOrDefault(x => x.ViewFamily == ViewFamily.ThreeDimensional);
            if (vft == null) return;
            var old = new FilteredElementCollector(doc).OfClass(typeof(View3D))
                .Cast<View3D>().FirstOrDefault(v => v.Name == "EXPORTACION_SAT");
            if (old != null) doc.Delete(old.Id);
            var v = View3D.CreateIsometric(doc, vft.Id);
            v.Name = "EXPORTACION_SAT"; v.DisplayStyle = DisplayStyle.Shading;
            v.IsolateElementsTemporary(gen); v.ConvertTemporaryHideIsolateToPermanent();
        }
    }

    // ════════════════════════════════════════════════════════
    //  CMD 2 — COMPARAR VERSIONES
    // ════════════════════════════════════════════════════════
    internal enum AccionComp { GuardarOLD, CompararYGenerar, Limpiar }

    [Transaction(TransactionMode.Manual)]
    public class CmdCompararVersiones : IExternalCommand
    {
        public Result Execute(ExternalCommandData data, ref string msg, ElementSet es)
        {
            var doc   = data.Application.ActiveUIDocument.Document;
            var uidoc = data.Application.ActiveUIDocument;
            string reg = Utils.RutaRegistro(doc);
            
            using (var dlg = new VentanaComparar(reg, BuscarOLD))
            {
                if (dlg.ShowDialog() != DialogResult.OK) return Result.Cancelled;
                reg = dlg.RutaElegida;
                string old = BuscarOLD(reg);
                
                if (dlg.Op == AccionComp.GuardarOLD)      return GuardarOLD(doc, reg);
                if (dlg.Op == AccionComp.CompararYGenerar)
                {
                    if (old == null)
                    { TaskDlg.Show("Sin OLD", "Guarda una version OLD primero."); return Result.Cancelled; }
                    return Comparar(doc, uidoc, reg, old, dlg.UsarGemini);
                }
                Limpiar(doc);
                TaskDlg.Show("Listo", "Colores eliminados.");
            }
            return Result.Succeeded;
        }

        string BuscarOLD(string reg)
        {
            if (!Directory.Exists(reg)) return null;
            if (Path.GetFileName(reg).Equals("OLD", StringComparison.OrdinalIgnoreCase)) return reg;
            if (Directory.Exists(Path.Combine(reg, "OLD"))) return Path.Combine(reg, "OLD");

            var c = Directory.GetDirectories(reg)
                .Where(d => Directory.Exists(Path.Combine(d, "OLD")))
                .OrderByDescending(d => d).FirstOrDefault();
            return c != null ? Path.Combine(c, "OLD") : null;
        }

        Result GuardarOLD(Document doc, string reg)
        {
            string stamp = DateTime.Now.ToString("yyyy-MM-dd"); // un registro por dia, se sobreescribe
            string path  = Path.Combine(reg, stamp, "OLD");

            // Crear carpeta
            Directory.CreateDirectory(path);

            // Guardar hashes CSV (esencial para la comparacion, no requiere transaccion)
            var sb = new StringBuilder();
            sb.AppendLine("UniqueId|Categoria|Tipo|Hash|Params");
            foreach (Element e in new FilteredElementCollector(doc, doc.ActiveView.Id).WhereElementIsNotElementType())
            {
                if (!Utils.EsPermitido(e)) continue;
                var pars = HashElem.ParamsLegibles(e);
                sb.AppendLine(e.UniqueId + "|" + e.Category?.Name + "|" + e.Name + "|" +
                    HashElem.Calcular(e) + "|" +
                    string.Join(";", pars.Select(kv => kv.Key + "=" + kv.Value)));
            }
            File.WriteAllText(Path.Combine(path, "hashes.csv"), sb.ToString(), Encoding.UTF8);
            Log(Path.Combine(reg, stamp), "OLD guardado", doc, 0, 0, 0);
            TaskDlg.Show("OLD Guardado", "Snapshot guardado en:\n" + path);
            return Result.Succeeded;
        }

        Result Comparar(Document doc, UIDocument uidoc, string reg, string oldDir, bool usarGemini)
        {
            string hf = Path.Combine(oldDir, "hashes.csv");
            if (!File.Exists(hf))
            { TaskDlg.Show("Error", "No se encontro hashes.csv en:\n" + oldDir); return Result.Failed; }

            var ant = new Dictionary<string, (string hash, string cat, string tipo, Dictionary<string, string> pars)>();
            foreach (var l in File.ReadAllLines(hf).Skip(1))
            {
                var p = l.Split('|');
                if (p.Length < 5) continue;
                var pars = p[4].Split(';').Where(x => x.Contains("="))
                    .ToDictionary(x => x.Split('=')[0],
                        x => x.Split(new[] { '=' }, 2).Length > 1 ? x.Split(new[] { '=' }, 2)[1] : "");
                ant[p[0]] = (p[3], p[1], p[2], pars);
            }

            var act = new FilteredElementCollector(doc, doc.ActiveView.Id).WhereElementIsNotElementType()
                .Where(Utils.EsPermitido).ToDictionary(e => e.UniqueId);

            var nuevos = act.Where(kv => !ant.ContainsKey(kv.Key)).Select(kv => kv.Value).ToList();
            var mods   = act.Where(kv => ant.ContainsKey(kv.Key) &&
                HashElem.Calcular(kv.Value) != ant[kv.Key].hash).Select(kv => kv.Value).ToList();
            var elims  = ant.Keys.Where(uid => !act.ContainsKey(uid)).ToList();

            // ── PASO 1: Si existe COMPARACION_BIM, cambiar la vista activa ANTES de abrir
            //   cualquier transaccion (uidoc.ActiveView no puede cambiarse dentro de Transaction)
            RevView viewOrigen = doc.ActiveView;
            var existenteVieja = new FilteredElementCollector(doc).OfClass(typeof(View3D))
                .Cast<View3D>().FirstOrDefault(v => v.Name == "COMPARACION_BIM");
            if (existenteVieja != null)
            {
                var otra = new FilteredElementCollector(doc).OfClass(typeof(View3D))
                    .Cast<View3D>().FirstOrDefault(v => v.Id != existenteVieja.Id && !v.IsTemplate);
                if (otra != null) uidoc.ActiveView = otra;
            }

            // ── PASO 2: Crear/duplicar la vista COMPARACION_BIM dentro de transaccion
            View3D vistaComp = null;
            using (var t = new Transaction(doc, "Crear Vista Comparacion"))
            {
                t.Start();
                if (existenteVieja != null) doc.Delete(existenteVieja.Id);

                if (viewOrigen is View3D act3D && viewOrigen.Name != "COMPARACION_BIM")
                {
                    vistaComp = doc.GetElement(act3D.Duplicate(ViewDuplicateOption.Duplicate)) as View3D;
                }
                else
                {
                    var vft = new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType))
                        .Cast<ViewFamilyType>().FirstOrDefault(x => x.ViewFamily == ViewFamily.ThreeDimensional);
                    vistaComp = View3D.CreateIsometric(doc, vft.Id);
                }
                vistaComp.Name = "COMPARACION_BIM";
                vistaComp.ViewTemplateId = ElementId.InvalidElementId;
                vistaComp.DisplayStyle = DisplayStyle.ShadingWithEdges;
                vistaComp.SaveOrientationAndLock();
                t.Commit();
            }

            // ── PASO 3: Colorear cambios (dentro de transaccion)
            var patron = new FilteredElementCollector(doc).OfClass(typeof(FillPatternElement))
                .Cast<FillPatternElement>().FirstOrDefault(fp => fp.GetFillPattern().IsSolidFill);

            using (var t = new Transaction(doc, "Colorear Cambios"))
            {
                t.Start();
                foreach (var e in act.Values)
                    try { vistaComp.SetElementOverrides(e.Id, new OverrideGraphicSettings()); } catch { }
                Colorear(vistaComp, nuevos, Utils.Verde,    patron);
                Colorear(vistaComp, mods,   Utils.Amarillo, patron);

                var idsConCambio = nuevos.Select(e => e.Id)
                    .Concat(mods.Select(e => e.Id)).ToHashSet();
                var oTrans = new OverrideGraphicSettings();
                oTrans.SetSurfaceTransparency(5);
                oTrans.SetProjectionLineColor(new RevColor(80, 100, 120));
                foreach (var e in act.Values)
                    if (!idsConCambio.Contains(e.Id))
                        try { vistaComp.SetElementOverrides(e.Id, oTrans); } catch { }
                Utils.OcultarCategoriasRuido(doc, vistaComp);
                t.Commit();
            }

            // ── PASO 4: Export IFC del NEW (fuera de transaccion, sin cambiar vista activa)
            string stamp  = Path.GetFileName(Path.GetDirectoryName(oldDir));
            string sesion = Path.Combine(reg, stamp);
            try { Utils.ExportarIFC(doc, Path.Combine(sesion, "NEW"), Utils.Safe(doc.Title) + "_NEW"); } catch { }

            // ── PASO 5: Activar la vista COMPARACION_BIM (operacion UI, nunca dentro de Transaction)
            uidoc.ActiveView = vistaComp;

            // Reporte con Gemini
            var lista = new List<(string estado, string desc, string cat, string tipo, string uid, string famTipo, string nivel, string marca)>();
            foreach (var e in nuevos)
            {
                var pd = HashElem.ParamsLegibles(e);
                string ft = pd.ContainsKey("Familia y Tipo") ? pd["Familia y Tipo"] : "";
                string nv = pd.ContainsKey("Nivel / Restriccion") ? pd["Nivel / Restriccion"] : "";
                string mc = pd.ContainsKey("Marca de Ubicacion") ? pd["Marca de Ubicacion"] : "-";
                lista.Add(("NUEVO", Gemini.NuevoDesc(e.Category?.Name ?? "", ft),
                    e.Category?.Name ?? "", e.Name, e.UniqueId, ft, nv, mc));
            }
            foreach (var e in mods)
            {
                var pa = ant.ContainsKey(e.UniqueId) ? ant[e.UniqueId].pars : new Dictionary<string, string>();
                var pd = HashElem.ParamsLegibles(e);
                string ft = pd.ContainsKey("Familia y Tipo") ? pd["Familia y Tipo"] : "";
                string nv = pd.ContainsKey("Nivel / Restriccion") ? pd["Nivel / Restriccion"] : "";
                string mc = pd.ContainsKey("Marca de Ubicacion") ? pd["Marca de Ubicacion"] : "-";
                string descMod = usarGemini
                    ? Gemini.Describir(e.Category?.Name ?? "", e.Name, pa, pd, nv, ft)
                    : Gemini.AutoDesc(e.Category?.Name ?? "", e.Name, pa, pd, ft);
                lista.Add(("MODIFICADO", descMod,
                    e.Category?.Name ?? "", e.Name, e.UniqueId, ft, nv, mc));
            }
            foreach (var uid in elims)
            {
                var pa = ant[uid].pars;
                string ft = pa.ContainsKey("Familia y Tipo") ? pa["Familia y Tipo"] : ant[uid].cat + " - " + ant[uid].tipo;
                string nv = pa.ContainsKey("Nivel / Restriccion") ? pa["Nivel / Restriccion"] : "Sin nivel";
                string mc = pa.ContainsKey("Marca de Ubicacion") ? pa["Marca de Ubicacion"] : "-";
                lista.Add(("ELIMINADO", Gemini.ElimDesc(ant[uid].cat, ft),
                    ant[uid].cat, ant[uid].tipo, uid, ft, nv, mc));
            }

            GenerarReporte(sesion, doc.Title, lista);
            Log(sesion, "Comparacion completada", doc, nuevos.Count, mods.Count, elims.Count);

            TaskDlg.Show("Comparacion Completada",
                "Vista COMPARACION_BIM activa\n\n" +
                "Nuevos:      " + nuevos.Count + "\n" +
                "Modificados: " + mods.Count + "\n" +
                "Eliminados:  " + elims.Count + "\n\n" +
                "Reporte en:\n" + sesion);
            return Result.Succeeded;
        }

        void Colorear(RevView v, List<Element> elems, RevColor col, FillPatternElement pat)
        {
            var o = new OverrideGraphicSettings();
            o.SetProjectionLineColor(col); o.SetSurfaceForegroundPatternColor(col);
            o.SetSurfaceForegroundPatternVisible(true);
            if (pat != null) o.SetSurfaceForegroundPatternId(pat.Id);
            o.SetSurfaceTransparency(5);
            foreach (var e in elems) try { v.SetElementOverrides(e.Id, o); } catch { }
        }

        void GenerarReporte(string sesion, string proyecto,
            List<(string estado, string desc, string cat, string tipo, string uid, string famTipo, string nivel, string marca)> lista)
        {
            string nomArchivo = "INFORME_BIM_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".xlsx";
            string rutaExcel  = Path.Combine(sesion, nomArchivo);
            string fecha      = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            string usuario    = Environment.UserName;
            int    totalCamb  = lista.Count;
            int    nNuevos    = lista.Count(c => c.estado == "NUEVO");
            int    nMods      = lista.Count(c => c.estado == "MODIFICADO");
            int    nElims     = lista.Count(c => c.estado == "ELIMINADO");
            string viewerUrl  = Config.Get("VIEWER_URL") ?? "";

            try
            {
                using (var wb = new ClosedXML.Excel.XLWorkbook())
                {
                    // ── PALETA CORPORATIVA ───────────────────────────────────
                    var cAzul      = ClosedXML.Excel.XLColor.FromHtml("#1CA0E6");
                    var cGrisOsc   = ClosedXML.Excel.XLColor.FromHtml("#2C3E50");
                    var cGrisMed   = ClosedXML.Excel.XLColor.FromHtml("#5D6D7E");
                    var cGrisClar  = ClosedXML.Excel.XLColor.FromHtml("#F7F9FC");
                    var cBorde     = ClosedXML.Excel.XLColor.FromHtml("#D5DDE5");
                    var cBlanco    = ClosedXML.Excel.XLColor.White;
                    var cTextoGris = ClosedXML.Excel.XLColor.FromHtml("#4A5568");
                    var cBgNuevo   = ClosedXML.Excel.XLColor.FromHtml("#E8F8F0");
                    var cTxNuevo   = ClosedXML.Excel.XLColor.FromHtml("#1A7A45");
                    var cBgMod     = ClosedXML.Excel.XLColor.FromHtml("#FFFDE7");
                    var cTxMod     = ClosedXML.Excel.XLColor.FromHtml("#7A5200");
                    var cBgElim    = ClosedXML.Excel.XLColor.FromHtml("#FDE8E8");
                    var cTxElim    = ClosedXML.Excel.XLColor.FromHtml("#7A1A1A");
                    var cAzulLink  = ClosedXML.Excel.XLColor.FromHtml("#0D6EFD");

                    // ── HOJA 1: INFORME BIM ──────────────────────────────────
                    var ws = wb.Worksheets.Add("INFORME BIM");
                    ws.ShowGridLines = false;

                    // Fila 1-3: Logo, Sistema, Version, Pagina
                    var rLogo = ws.Range("A1:C3"); rLogo.Merge();
                    rLogo.Style.Fill.BackgroundColor = cBlanco;
                    string logoPath = @"C:\ProgramData\Autodesk\Revit\Macros\2025\Revit\AppHookup\PluginEstructural\Source\PluginEstructural\Desing\Logo postensa.png";
                    if (System.IO.File.Exists(logoPath))
                    {
                        var pic = ws.AddPicture(logoPath).MoveTo(ws.Cell(1, 1), 180, 15);
                        pic.Width = 160; pic.Height = 45;
                    }

                    var rCod = ws.Range("D1:F3");
                    rCod.Merge();
                    rCod.Value = "CÓDIGO\r\nR3PI6";
                    rCod.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                    rCod.Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;
                    rCod.Style.Alignment.WrapText = true;
                    rCod.Style.Font.Bold = true;
                    rCod.Style.Font.FontSize = 12;
                    rCod.Style.Font.FontColor = ClosedXML.Excel.XLColor.Black;
                    rCod.Style.Fill.BackgroundColor = cBlanco;
                    
                    ws.Cell("G1").Value = "Sistema:";    ws.Cell("H1").Value = "SGI";
                    ws.Cell("G2").Value = "Versión:";    ws.Cell("H2").Value = "02";
                    ws.Cell("G3").Value = "Página:";     ws.Cell("H3").Value = "01";

                    var rSys = ws.Range("G1:H3");
                    rSys.Style.Font.Bold = true;
                    rSys.Style.Font.FontSize = 10;
                    rSys.Style.Font.FontColor = ClosedXML.Excel.XLColor.Black;
                    rSys.Style.Fill.BackgroundColor = cBlanco;
                    rSys.Style.Border.InsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                    rSys.Style.Border.InsideBorderColor = ClosedXML.Excel.XLColor.Black;
                    rSys.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                    rSys.Style.Border.OutsideBorderColor = ClosedXML.Excel.XLColor.Black;
                    
                    for (int r = 1; r <= 3; r++) { ws.Cell(r, 7).Style.Fill.BackgroundColor = cGrisClar; ws.Cell(r, 8).Style.Fill.BackgroundColor = cBlanco; }
                    ws.Cell("H1").Style.Font.Bold = false; ws.Cell("H2").Style.Font.Bold = false; ws.Cell("H3").Style.Font.Bold = false;

                    rLogo.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                    rLogo.Style.Border.OutsideBorderColor = ClosedXML.Excel.XLColor.Black;
                    ws.Range("D1:F3").Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                    ws.Range("D1:F3").Style.Border.OutsideBorderColor = ClosedXML.Excel.XLColor.Black;

                    // Fila 4: Titulo INFORME DE INCIDENCIAS
                    var rTit = ws.Range("A4:H4"); rTit.Merge();
                    rTit.Value = "INFORME DE INCIDENCIAS";
                    rTit.Style.Font.Bold        = true;
                    rTit.Style.Font.FontSize    = 16;
                    rTit.Style.Font.FontColor   = ClosedXML.Excel.XLColor.FromHtml("#002060");
                    rTit.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                    rTit.Style.Alignment.Vertical   = ClosedXML.Excel.XLAlignmentVerticalValues.Center;
                    rTit.Style.Fill.BackgroundColor = cBlanco;
                    rTit.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                    rTit.Style.Border.OutsideBorderColor = ClosedXML.Excel.XLColor.Black;
                    
                    ws.Row(1).Height = 20; ws.Row(2).Height = 20; ws.Row(3).Height = 20; ws.Row(4).Height = 25;

                    // Fila 5: banda azul
                    var rBanda = ws.Range("A5:H5"); rBanda.Merge();
                    rBanda.Style.Fill.BackgroundColor = cAzul;
                    ws.Row(5).Height = 4;

                    // Filas 6-8: info del reporte
                    void InfoFila(int fila, string k1, string v1, string k2, object v2, bool esLink2 = false)
                    {
                        ws.Cell(fila, 1).Value = k1;
                        ws.Cell(fila, 2).Value = v1;
                        ws.Range(fila, 2, fila, 4).Merge();
                        ws.Cell(fila, 5).Value = k2;
                        if (esLink2 && v2 is string urlStr && !string.IsNullOrEmpty(urlStr))
                        {
                            ws.Cell(fila, 6).Value = "Abrir Viewer";
                            try { ws.Cell(fila, 6).SetHyperlink(new ClosedXML.Excel.XLHyperlink(urlStr)); } catch { ws.Cell(fila, 6).Value = urlStr; }
                            ws.Cell(fila, 6).Style.Font.FontColor = cAzulLink;
                            ws.Cell(fila, 6).Style.Font.Underline = ClosedXML.Excel.XLFontUnderlineValues.Single;
                        }
                        else
                            ws.Cell(fila, 6).Value = v2?.ToString() ?? "";
                        ws.Range(fila, 6, fila, 8).Merge();
                        var r = ws.Range(fila, 1, fila, 8);
                        r.Style.Fill.BackgroundColor = cGrisClar;
                        r.Style.Font.FontSize = 9;
                        foreach (int c in new[] { 1, 5 })
                        {
                            ws.Cell(fila, c).Style.Font.Bold     = true;
                            ws.Cell(fila, c).Style.Font.FontColor = cGrisMed;
                        }
                        ws.Cell(fila, 2).Style.Font.FontColor = cTextoGris;
                    }
                    InfoFila(6, "Proyecto:",  proyecto, "Fecha:",   "'" + fecha);
                    InfoFila(7, "Usuario:",   usuario,  "Viewer:",  viewerUrl, esLink2: true);
                    InfoFila(8, "Generado por:", "Plugin BIM Estructural v2.1 — POSTENSA", "Resumen:",
                        "+" + nNuevos + " Nuevos  ~" + nMods + " Mod.  -" + nElims + " Elim.");
                    ws.Row(6).Height = 15; ws.Row(7).Height = 15; ws.Row(8).Height = 15;

                    // Fila 9: separador
                    ws.Row(9).Height = 5;

                    // Fila 10: encabezados de tabla
                    // 8 columnas: N° | Estado | Descripción del Cambio | Categoría | Familia y Tipo | Nivel | Marca de ubicación | UniqueId
                    var hdrs = new[] { "N°", "Estado", "Descripción del Cambio", "Categoría",
                        "Familia y Tipo", "Nivel / Restricción", "Marca de ubicación", "UniqueId" };
                    for (int c = 1; c <= 8; c++)
                    {
                        var cel = ws.Cell(10, c);
                        cel.Value = hdrs[c - 1];
                        cel.Style.Fill.BackgroundColor = cGrisOsc;
                        cel.Style.Font.FontColor = cBlanco;
                        cel.Style.Font.Bold      = true;
                        cel.Style.Font.FontSize  = 10;
                        cel.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                        cel.Style.Alignment.Vertical   = ClosedXML.Excel.XLAlignmentVerticalValues.Center;
                        cel.Style.Border.BottomBorder  = ClosedXML.Excel.XLBorderStyleValues.Thin;
                        cel.Style.Border.BottomBorderColor = cAzul;
                    }
                    ws.Row(10).Height = 22;

                    // Datos
                    int fila = 11;
                    int num  = 1;
                    foreach (var orden in new[] { "NUEVO", "MODIFICADO", "ELIMINADO" })
                    {
                        foreach (var item in lista.Where(x => x.estado == orden))
                        {
                            bool par  = (fila % 2 == 0);
                            var bgRow = par ? cBlanco : cGrisClar;

                            ws.Cell(fila, 1).Value = num++;
                            ws.Cell(fila, 1).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                            ws.Cell(fila, 1).Style.Font.FontColor = cTextoGris;

                            var celEst = ws.Cell(fila, 2);
                            celEst.Value = item.estado;
                            celEst.Style.Font.Bold = true;
                            celEst.Style.Font.FontSize = 9;
                            celEst.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                            if (item.estado == "NUEVO")           { celEst.Style.Fill.BackgroundColor = cBgNuevo; celEst.Style.Font.FontColor = cTxNuevo; }
                            else if (item.estado == "MODIFICADO") { celEst.Style.Fill.BackgroundColor = cBgMod;   celEst.Style.Font.FontColor = cTxMod;   }
                            else                                   { celEst.Style.Fill.BackgroundColor = cBgElim;  celEst.Style.Font.FontColor = cTxElim;  }

                            ws.Cell(fila, 3).Value = item.desc;
                            ws.Cell(fila, 4).Value = item.cat;
                            ws.Cell(fila, 5).Value = item.famTipo;
                            ws.Cell(fila, 6).Value = item.nivel;

                            // Columna 7: Marca de ubicación
                            ws.Cell(fila, 7).Value = item.marca;
                            ws.Cell(fila, 7).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

                            ws.Cell(fila, 8).Value = item.uid;

                            foreach (int col in new[] { 1, 3, 4, 5, 6, 7, 8 })
                            {
                                var cel = ws.Cell(fila, col);
                                cel.Style.Fill.BackgroundColor = bgRow;
                                cel.Style.Font.FontColor = cTextoGris;
                                cel.Style.Border.BottomBorder = ClosedXML.Excel.XLBorderStyleValues.Hair;
                                cel.Style.Border.BottomBorderColor = cBorde;
                            }
                            celEst.Style.Border.BottomBorder = ClosedXML.Excel.XLBorderStyleValues.Hair;
                            celEst.Style.Border.BottomBorderColor = cBorde;
                            ws.Cell(fila, 3).Style.Alignment.WrapText = true;
                            ws.Row(fila).Height = 30;
                            fila++;
                        }
                    }

                    // Banda azul de cierre + fila resumen
                    ws.Row(fila).Height = 4;
                    ws.Range(fila, 1, fila, 8).Style.Fill.BackgroundColor = cAzul;
                    fila++;

                    ws.Range(fila, 1, fila, 8).Style.Fill.BackgroundColor = cGrisClar;
                    ws.Range(fila, 1, fila, 8).Style.Font.Bold = true;
                    ws.Cell(fila, 1).Value = "RESUMEN →";
                    ws.Cell(fila, 1).Style.Font.FontColor = cGrisMed;
                    ws.Cell(fila, 2).Value = "+" + nNuevos + " Nuevos";
                    ws.Cell(fila, 2).Style.Font.FontColor = cTxNuevo;
                    ws.Cell(fila, 3).Value = "~" + nMods + " Modificados";
                    ws.Cell(fila, 3).Style.Font.FontColor = cTxMod;
                    ws.Cell(fila, 4).Value = "-" + nElims + " Eliminados";
                    ws.Cell(fila, 4).Style.Font.FontColor = cTxElim;
                    ws.Cell(fila, 5).Value = "TOTAL: " + totalCamb;
                    ws.Cell(fila, 5).Style.Font.FontColor = cGrisOsc;
                    ws.Row(fila).Height = 20;

                    // Anchos de columna optimizados
                    ws.Column(1).Width =  5;  // N°
                    ws.Column(2).Width = 14;  // Estado
                    ws.Column(3).Width = 55;  // Descripción
                    ws.Column(4).Width = 22;  // Categoría
                    ws.Column(5).Width = 38;  // Familia y Tipo
                    ws.Column(6).Width = 26;  // Nivel
                    ws.Column(7).Width = 16;  // Viewer link
                    ws.Column(8).Width = 40;  // UniqueId

                    // Freeze filas de encabezado (1-10)
                    ws.SheetView.FreezeRows(10);

                    // Borde exterior de la tabla
                    ws.Range(10, 1, fila, 8).Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Medium;
                    ws.Range(10, 1, fila, 8).Style.Border.OutsideBorderColor = cGrisOsc;

                    wb.SaveAs(rutaExcel);
                }
                try { System.Diagnostics.Process.Start(rutaExcel); } catch { }
            }
            catch
            {
                // Fallback CSV
                var sb = new StringBuilder();
                sb.AppendLine("No.|Estado|Descripcion|Categoria|FamiliaYTipo|Nivel|UniqueId");
                int idx = 1;
                foreach (var item in lista)
                    sb.AppendLine(idx++ + "|" + item.estado + "|" + item.desc + "|" + item.cat + "|" + item.famTipo + "|" + item.nivel + "|" + item.uid);
                File.WriteAllText(Path.Combine(sesion, "INFORME_BIM.csv"), sb.ToString(), Encoding.UTF8);
            }
        }







        void Limpiar(Document doc)
        {
            using (var t = new Transaction(doc, "Limpiar Colores"))
            {
                t.Start();
                var v = doc.ActiveView;
                foreach (Element e in new FilteredElementCollector(doc, v.Id).WhereElementIsNotElementType())
                    if (Utils.EsPermitido(e))
                        try { v.SetElementOverrides(e.Id, new OverrideGraphicSettings()); } catch { }
                t.Commit();
            }
        }

        void Log(string sesion, string accion, Document doc, int n, int m, int el)
        {
            string f = Path.Combine(sesion, "auditoria.log");
            var sb = new StringBuilder();
            if (File.Exists(f)) sb.Append(File.ReadAllText(f));
            sb.AppendLine("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] " +
                accion + " — " + doc.Title + " — " + Environment.UserName);
            if (n > 0 || m > 0 || el > 0)
                sb.AppendLine("  N:" + n + " M:" + m + " E:" + el);
            File.WriteAllText(f, sb.ToString(), Encoding.UTF8);
        }
    }

    // ════════════════════════════════════════════════════════
    //  VENTANA SAT
    // ════════════════════════════════════════════════════════
    internal class VentanaSAT : System.Windows.Forms.Form
    {
        public bool         UsarSeleccion { get; private set; }
        public List<string> Niveles       { get; private set; } = new List<string>();
        public string       RutaExportacion { get; private set; }
        public string       NombreArchivo { get; private set; }
        RadioButton rbSel, rbTodo, rbNiv;
        CheckedListBox clb;

        // Persistir ultima ruta de exportacion durante la sesion de Revit
        static string _ultimaRuta = null;

        public VentanaSAT(Document doc)
        {
            Text = "Exportar SAT"; Size = new WinSize(468, 634);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false;
            BackColor = Esti.FondoOscuro; ForeColor = Esti.TextoPrincipal;
            Controls.Add(Esti.HeaderLogo(468));
            Controls.Add(Esti.Titulo("EXPORTAR SOLIDO SAT", 16, 80));
            Controls.Add(Esti.Lbl("Geometria unificada con rotacion -90 para SolidWorks.", 16, 108));
            Controls.Add(Esti.Sep(132));
            
            Controls.Add(Esti.Lbl("Carpeta de exportacion:", 16, 142));
            string rutaDefault = _ultimaRuta ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Exportacion_SAT");
            var txtRuta = new System.Windows.Forms.TextBox { Location = new WinPoint(16, 164), Size = new WinSize(390, 24), Font = Esti.FNormal, ReadOnly = true };
            txtRuta.Text = rutaDefault;
            var btnRuta = new WinButton { Text = "...", Location = new WinPoint(412, 163), Size = new WinSize(36, 25), Font = Esti.FBold, BackColor = Esti.ColGris, ForeColor = WinColor.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnRuta.FlatAppearance.BorderSize = 0;
            btnRuta.Click += (s, e) => {
                using (var fbd = new FolderBrowserDialog { Description = "Carpeta de Exportacion SAT", SelectedPath = txtRuta.Text }) {
                    if (fbd.ShowDialog() == DialogResult.OK) txtRuta.Text = fbd.SelectedPath;
                }
            };
            Controls.AddRange(new System.Windows.Forms.Control[] { txtRuta, btnRuta });

            Controls.Add(Esti.Lbl("Nombre del archivo (sin .sat):", 16, 196));
            var txtNombre = new System.Windows.Forms.TextBox { Location = new WinPoint(16, 218), Size = new WinSize(390, 24), Font = Esti.FNormal };
            txtNombre.Text = Utils.Safe(doc.Title) + "_SAT";

            Controls.Add(Esti.Lbl("Alcance:", 16, 256));
            rbSel  = RB("Usar seleccion activa", 16, 278, true);
            rbTodo = RB("Todo el modelo estructural", 16, 302);
            rbNiv  = RB("Filtrar por niveles:", 16, 326);
            rbNiv.CheckedChanged += (s, e) => clb.Enabled = rbNiv.Checked;
            clb = new CheckedListBox
            {
                Location = new WinPoint(32, 350), Size = new WinSize(400, 110),
                Enabled = false, BackColor = Esti.FondoMedio,
                ForeColor = Esti.TextoPrincipal, Font = Esti.FNormal, BorderStyle = BorderStyle.None
            };
            foreach (Level lv in new FilteredElementCollector(doc).OfClass(typeof(Level))
                .Cast<Level>().OrderBy(l => l.Elevation))
                clb.Items.Add(lv.Name);
            Controls.Add(Esti.Sep(480));
            Controls.Add(Esti.PanelInfo(
                "Exportacion SAT automatica a la ruta indicada tras generar vista.",
                490, Esti.Acento, 40));
            var bOK  = Esti.Btn("Exportar", Esti.Acento,  244, 540, 130, 32);
            var bCan = Esti.Btn("Cancelar", Esti.ColGris, 382, 540,  72, 32);
            bOK.Click += (s, e) =>
            {
                UsarSeleccion = rbSel.Checked;
                if (rbNiv.Checked) Niveles = clb.CheckedItems.Cast<string>().ToList();
                RutaExportacion = txtRuta.Text;
                NombreArchivo = string.IsNullOrWhiteSpace(txtNombre.Text) ? Utils.Safe(doc.Title) + "_SAT" : Utils.Safe(txtNombre.Text);
                _ultimaRuta = txtRuta.Text;   // guardar para la proxima vez
                DialogResult = DialogResult.OK; Close();
            };
            bCan.DialogResult = DialogResult.Cancel;
            Controls.AddRange(new System.Windows.Forms.Control[] { rbSel, rbTodo, rbNiv, clb, bOK, bCan });
            AcceptButton = bOK; CancelButton = bCan;
        }

        RadioButton RB(string t, int x, int y, bool ch = false) => new RadioButton
        {
            Text = t, Location = new WinPoint(x, y), AutoSize = true, Checked = ch,
            ForeColor = Esti.TextoPrincipal, Font = Esti.FNormal, BackColor = WinColor.Transparent
        };
    }

    // ════════════════════════════════════════════════════════
    //  VENTANA COMPARAR
    // ════════════════════════════════════════════════════════
    internal class VentanaComparar : System.Windows.Forms.Form
    {
        public AccionComp Op { get; private set; }
        public string RutaElegida { get; private set; }
        public bool UsarGemini { get; private set; }

        // Ultima ruta usada en la sesion de Revit
        static string _ultimaRuta = null;

        public VentanaComparar(string regActual, Func<string, string> funcBuscarOLD)
        {
            string ruta = _ultimaRuta ?? regActual;
            Text = "Comparar Versiones"; Size = new WinSize(468, 580);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false;
            BackColor = Esti.FondoOscuro; ForeColor = Esti.TextoPrincipal;
            Controls.Add(Esti.HeaderLogo(468));
            Controls.Add(Esti.Titulo("COMPARADOR DE VERSIONES", 16, 80));
            Controls.Add(Esti.Lbl("Registro OLD/NEW con reporte y vista limpia.", 16, 108));
            Controls.Add(Esti.Sep(132));

            Controls.Add(Esti.Lbl("Carpeta de registro de modelos:", 16, 142));
            var txtRuta = new System.Windows.Forms.TextBox { Location = new WinPoint(16, 164), Size = new WinSize(390, 24), Font = Esti.FNormal, ReadOnly = true };
            txtRuta.Text = ruta;
            RutaElegida = ruta;
            var btnRuta = new WinButton { Text = "...", Location = new WinPoint(412, 163), Size = new WinSize(36, 25), Font = Esti.FBold, BackColor = Esti.ColGris, ForeColor = WinColor.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnRuta.FlatAppearance.BorderSize = 0;
            Controls.AddRange(new System.Windows.Forms.Control[] { txtRuta, btnRuta });

            var lblEst = Esti.Lbl("", 16, 202);
            Controls.Add(lblEst);

            Controls.Add(Esti.PanelInfo(
                "Flujo:  [Guardar OLD]  >  [Cambios en Revit]  >  [Comparar y generar NEW]",
                224, Esti.Acento, 34));

            // ── CheckBox Gemini (desactivado por defecto para ahorrar tokens) ──
            var chkGemini = new CheckBox
            {
                Text = "Incluir descripciones con IA (Gemini)  — consume tokens",
                Location = new WinPoint(16, 268), AutoSize = true, Checked = false,
                ForeColor = Esti.TextoSecundario, Font = Esti.FPeque, BackColor = WinColor.Transparent
            };
            Controls.Add(chkGemini);

            var bG = Esti.BtnGrande("Guardar version OLD\n    Exporta IFC + estado actual como linea base", 300);
            var bC = Esti.BtnGrande(
                "Comparar con OLD y generar NEW\n    Colorea + vista limpia + reporte + auditoria",
                364, false, Esti.FondoPanel);
            var bL = Esti.BtnGrande("Limpiar colores de la vista activa", 428, true, Esti.ColGris);

            Action actualizarEstado = () => {
                string rOld = funcBuscarOLD(txtRuta.Text);
                bool hay = (rOld != null);
                if (hay)
                {
                    string dirInfo = Path.GetFileName(Path.GetDirectoryName(rOld));
                    if (string.IsNullOrEmpty(dirInfo) || (rOld.EndsWith("OLD", StringComparison.OrdinalIgnoreCase) && Path.GetDirectoryName(rOld) != null))
                        dirInfo = Path.GetFileName(Path.GetDirectoryName(rOld));
                    lblEst.Text = "Version OLD encontrada: " + dirInfo;
                }
                else
                {
                    lblEst.Text = "Sin version OLD — guarda una antes de comparar";
                }
                lblEst.ForeColor = hay ? Esti.ColVerde : Esti.ColAmarillo;
                bC.Enabled = hay;
                bC.FlatAppearance.BorderColor = hay ? Esti.Acento : Esti.ColGris;
                bC.ForeColor = hay ? Esti.TextoPrincipal : Esti.TextoSecundario;
            };

            actualizarEstado();

            btnRuta.Click += (s, e) => {
                using (var fbd = new FolderBrowserDialog { Description = "Selecciona la carpeta de Registro BIM (o subcarpeta de version especifica)", SelectedPath = txtRuta.Text }) {
                    if (fbd.ShowDialog() == DialogResult.OK) {
                        txtRuta.Text = fbd.SelectedPath;
                        RutaElegida = fbd.SelectedPath;
                        _ultimaRuta = fbd.SelectedPath;
                        actualizarEstado();
                    }
                }
            };

            bG.Click += (s, e) => { _ultimaRuta = RutaElegida; UsarGemini = chkGemini.Checked; Op = AccionComp.GuardarOLD;       DialogResult = DialogResult.OK; Close(); };
            bC.Click += (s, e) => { _ultimaRuta = RutaElegida; UsarGemini = chkGemini.Checked; Op = AccionComp.CompararYGenerar; DialogResult = DialogResult.OK; Close(); };
            bL.Click += (s, e) => { _ultimaRuta = RutaElegida; Op = AccionComp.Limpiar;          DialogResult = DialogResult.OK; Close(); };
            Controls.Add(Esti.Sep(494));
            var bCan = Esti.Btn("Cancelar", Esti.ColGris, 384, 502, 70, 28);
            bCan.DialogResult = DialogResult.Cancel;
            Controls.AddRange(new System.Windows.Forms.Control[] { bG, bC, bL, bCan });
            CancelButton = bCan;
        }
    }

    // ════════════════════════════════════════════════════════
    //  CMD 3 — CORTE JERARQUICO
    // ════════════════════════════════════════════════════════
    internal enum AlcanceCorte { VistaActiva, TodoElModelo, SeleccionActiva }

    [Transaction(TransactionMode.Manual)]
    public class CmdCorteJerarquico : IExternalCommand
    {
        // Jerarquia: mayor prioridad corta a menor.
        // 0=Suelos/Losas, 1=Pilares/Columnas, 2=Vigas/Armazones, 3=Muros,
        // 4=Cubiertas, 5=Cimentaciones, 6=Escaleras/Rampas, 7=Varios/EstructurasTemporales
        static readonly List<BuiltInCategory>[] Jerarquia = new[]
        {
            new List<BuiltInCategory> { BuiltInCategory.OST_Floors },
            new List<BuiltInCategory> { BuiltInCategory.OST_StructuralColumns, BuiltInCategory.OST_Columns },
            new List<BuiltInCategory> { BuiltInCategory.OST_StructuralFraming, BuiltInCategory.OST_StructuralTruss },
            new List<BuiltInCategory> { BuiltInCategory.OST_Walls },
            new List<BuiltInCategory> { BuiltInCategory.OST_Roofs },
            new List<BuiltInCategory> { BuiltInCategory.OST_StructuralFoundation, BuiltInCategory.OST_StructConnections },
            new List<BuiltInCategory> { BuiltInCategory.OST_Stairs, BuiltInCategory.OST_Ramps },
            new List<BuiltInCategory> { BuiltInCategory.OST_GenericModel, BuiltInCategory.OST_Parts },
        };
        static readonly string[] NombresNivel =
        {
            "Suelos / Losas",
            "Pilares / Columnas",
            "Vigas / Armazones",
            "Muros",
            "Cubiertas",
            "Cimentaciones / Conexiones",
            "Escaleras / Rampas",
            "Estructuras Temporales / Varios",
        };

        public Result Execute(ExternalCommandData data, ref string msg, ElementSet es)
        {
            var uidoc = data.Application.ActiveUIDocument;
            var doc   = uidoc.Document;
            using (var dlg = new VentanaCorte())
            {
                if (dlg.ShowDialog() != DialogResult.OK) return Result.Cancelled;
                var niveles    = dlg.NivelesActivos;   // bool[7]
                var alcance    = dlg.Alcance;
                bool autoSwitch = dlg.AutoSwitch;

                // Recolectar grupos segun alcance
                Func<List<BuiltInCategory>, List<Element>> Colectar = cats =>
                {
                    var f = new ElementMulticategoryFilter(cats);
                    var col = alcance == AlcanceCorte.SeleccionActiva
                    ? new FilteredElementCollector(doc, uidoc.Selection.GetElementIds())
                    : alcance == AlcanceCorte.VistaActiva
                        ? new FilteredElementCollector(doc, doc.ActiveView.Id)
                        : new FilteredElementCollector(doc);
                    return col.WherePasses(f).WhereElementIsNotElementType().ToList();
                };

                var grupos = new List<Element>[Jerarquia.Length];
                for (int i = 0; i < Jerarquia.Length; i++)
                    grupos[i] = niveles[i] ? Colectar(Jerarquia[i]) : new List<Element>();

                int total = 0;
                using (var t = new Transaction(doc, "Corte Geometrico Jerarquico"))
                {
                    t.Start();
                    // Ejecutar jerarquia: nivel i corta a todos los niveles j > i
                    for (int i = 0; i < grupos.Length; i++)
                    {
                        if (!niveles[i] || grupos[i].Count == 0) continue;
                        for (int j = i; j < grupos.Length; j++)
                        {
                            if (!niveles[j] || grupos[j].Count == 0) continue;
                            foreach (var dom in grupos[i])
                                total += UnirGrupo(doc, dom, grupos[j], autoSwitch);
                        }
                    }
                    t.Commit();
                }

                TaskDlg.Show("Corte Geometrico",
                    "Corte jerarquico completado.\n" +
                    "Uniones procesadas: " + total + "\n" +
                    "Alcance: " + (alcance == AlcanceCorte.SeleccionActiva ? "Seleccion activa"
                                 : alcance == AlcanceCorte.VistaActiva    ? "Vista activa"
                                 : "Todo el modelo"));
            }
            return Result.Succeeded;
        }

        static int UnirGrupo(Document doc, Element dom, List<Element> subs, bool autoSwitch)
        {
            var bb = dom.get_BoundingBox(null);
            if (bb == null) return 0;
            // Expand bounding box slightly to catch touching elements
            double tol = 0.05;
            var min = new XYZ(bb.Min.X - tol, bb.Min.Y - tol, bb.Min.Z - tol);
            var max = new XYZ(bb.Max.X + tol, bb.Max.Y + tol, bb.Max.Z + tol);
            var bbf = new BoundingBoxIntersectsFilter(new Outline(min, max));
            int cnt = 0;
            foreach (var sub in subs)
            {
                if (dom.Id == sub.Id) continue;
                if (!bbf.PassesFilter(sub)) continue;
                try
                {
                    if (!JoinGeometryUtils.AreElementsJoined(doc, dom, sub))
                        JoinGeometryUtils.JoinGeometry(doc, dom, sub);
                    if (autoSwitch && !JoinGeometryUtils.IsCuttingElementInJoin(doc, dom, sub))
                        JoinGeometryUtils.SwitchJoinOrder(doc, dom, sub);
                    cnt++;
                }
                catch { }
            }
            return cnt;
        }
    }

    // ════════════════════════════════════════════════════════
    //  VENTANA CORTE JERARQUICO
    // ════════════════════════════════════════════════════════
    internal class VentanaCorte : System.Windows.Forms.Form
    {
        static readonly string[] NombresNivel =
        {
            "Suelos / Losas",
            "Pilares / Columnas",
            "Vigas / Armazones",
            "Muros",
            "Cubiertas",
            "Cimentaciones / Conexiones",
            "Escaleras / Rampas",
            "Estructuras Temporales / Varios",
        };

        public bool[]       NivelesActivos { get; private set; } = new bool[8];
        public AlcanceCorte Alcance        { get; private set; } = AlcanceCorte.VistaActiva;
        public bool         AutoSwitch     { get; private set; } = true;

        RadioButton rbVista, rbTodo, rbSel;
        CheckBox[]  chks = new CheckBox[8];
        CheckBox    chkSwitch;

        public VentanaCorte()
        {
            Text = "Corte Geometrico"; Size = new WinSize(468, 740);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false;
            BackColor = Esti.FondoOscuro; ForeColor = Esti.TextoPrincipal;
            Controls.Add(Esti.HeaderLogo(468));

            // ── Titulo ─────────────────────────────────────────
            Controls.Add(Esti.Titulo("CORTE GEOMETRICO JERARQUICO", 16, 80));
            Controls.Add(Esti.Lbl("Une y corta elementos estructurales segun su jerarquia.", 16, 108));
            Controls.Add(Esti.Sep(130));

            // ── Alcance ────────────────────────────────────────
            Controls.Add(Esti.Lbl("Alcance:", 16, 142));
            rbSel   = new RadioButton { Text = "Usar seleccion activa",
                Location = new WinPoint(16, 162), AutoSize = true, Checked = false,
                ForeColor = Esti.TextoPrincipal, Font = Esti.FNormal, BackColor = WinColor.Transparent };
            rbVista = new RadioButton { Text = "Vista activa (mas rapido)",
                Location = new WinPoint(16, 184), AutoSize = true, Checked = true,
                ForeColor = Esti.TextoPrincipal, Font = Esti.FNormal, BackColor = WinColor.Transparent };
            rbTodo  = new RadioButton { Text = "Todo el modelo",
                Location = new WinPoint(16, 206), AutoSize = true,
                ForeColor = Esti.TextoPrincipal, Font = Esti.FNormal, BackColor = WinColor.Transparent };
            Controls.AddRange(new System.Windows.Forms.Control[] { rbSel, rbVista, rbTodo });
            Controls.Add(Esti.Sep(228));

            // ── Niveles jerarquicos ────────────────────────────
            Controls.Add(Esti.Lbl("Niveles a procesar (de mayor a menor jerarquia):", 16, 240));
            // Colores
            WinColor[] colores = {
                WinColor.FromArgb(22,160,230),  // azul  — Suelos
                WinColor.FromArgb(39,201,111),  // verde — Pilares
                WinColor.FromArgb(255,196,0),   // amarillo — Vigas
                WinColor.FromArgb(255,138,60),  // naranja — Muros
                WinColor.FromArgb(229,57,53),   // rojo — Cubiertas
                WinColor.FromArgb(156,39,176),  // morado — Cimentacion
                WinColor.FromArgb(0,188,212),   // cian — Escaleras
                WinColor.FromArgb(100,110,120), // gris — Varios
            };
            for (int i = 0; i < 8; i++)
            {
                int y = 264 + i * 30;
                var dot = new WinPanel { Location = new WinPoint(20, y + 4),
                    Size = new WinSize(12, 12), BackColor = colores[i] };
                dot.Paint += (s, e) => {};
                var ck = new CheckBox { Text = (i + 1) + ". " + NombresNivel[i],
                    Location = new WinPoint(40, y), AutoSize = true, Checked = true,
                    ForeColor = Esti.TextoPrincipal, Font = Esti.FNormal, BackColor = WinColor.Transparent };
                Controls.Add(dot);
                Controls.Add(ck);
                chks[i] = ck;
            }

            // Boton seleccionar / deseleccionar todo
            var bAll = new WinButton { Text = "Todos",
                Location = new WinPoint(16, 510), Size = new WinSize(70, 24),
                Font = Esti.FPeque, BackColor = Esti.Acento, ForeColor = WinColor.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            bAll.FlatAppearance.BorderSize = 0;
            bAll.Click += (s, e) => { foreach (var c in chks) c.Checked = true; };
            var bNone = new WinButton { Text = "Ninguno",
                Location = new WinPoint(92, 510), Size = new WinSize(70, 24),
                Font = Esti.FPeque, BackColor = Esti.ColGris, ForeColor = WinColor.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            bNone.FlatAppearance.BorderSize = 0;
            bNone.Click += (s, e) => { foreach (var c in chks) c.Checked = false; };
            Controls.AddRange(new System.Windows.Forms.Control[] { bAll, bNone });

            Controls.Add(Esti.Sep(542));

            // ── Opciones ───────────────────────────────────────
            chkSwitch = new CheckBox { Text = "Corregir orden de corte automaticamente (SwitchJoinOrder)",
                Location = new WinPoint(16, 554), AutoSize = true, Checked = true,
                ForeColor = Esti.TextoSecundario, Font = Esti.FPeque, BackColor = WinColor.Transparent };
            Controls.Add(chkSwitch);

            // Info
            Controls.Add(Esti.PanelInfo(
                "El elemento de mayor jerarquia corta al de menor. Usa BoundingBox para optimizar rendimiento.",
                578, Esti.Acento, 36));

            // ── Botones ────────────────────────────────────────
            var bOK  = Esti.Btn("Ejecutar", Esti.Acento,  230, 626, 130, 32);
            var bCan = Esti.Btn("Cancelar", Esti.ColGris, 368, 626,  82, 32);
            bOK.Click += (s, e) =>
            {
                Alcance    = rbSel.Checked ? AlcanceCorte.SeleccionActiva
                           : rbVista.Checked ? AlcanceCorte.VistaActiva : AlcanceCorte.TodoElModelo;
                AutoSwitch = chkSwitch.Checked;
                for (int i = 0; i < 8; i++) NivelesActivos[i] = chks[i].Checked;
                DialogResult = DialogResult.OK; Close();
            };
            bCan.DialogResult = DialogResult.Cancel;
            Controls.AddRange(new System.Windows.Forms.Control[] { bOK, bCan });
            AcceptButton = bOK; CancelButton = bCan;
        }
    }

    // ════════════════════════════════════════════════════════
    //  CMD 4 — VISTA COMPARTIDA (Revit Shared Views)
    // ════════════════════════════════════════════════════════
    [Transaction(TransactionMode.Manual)]
    public class CmdCompartirVista : IExternalCommand
    {
        public Result Execute(ExternalCommandData data, ref string msg, ElementSet es)
        {
            var uidoc = data.Application.ActiveUIDocument;

            // Intentar ejecutar el comando interno de Revit "Shared Views"
            try
            {
                RevitCommandId cmdId = RevitCommandId.LookupPostableCommandId(
                    PostableCommand.SharedViews);
                if (cmdId != null && uidoc.Application.CanPostCommand(cmdId))
                {
                    uidoc.Application.PostCommand(cmdId);
                    return Result.Succeeded;
                }
            }
            catch { }

            // Fallback: abrir viewer.autodesk.com e instrucciones
            try { System.Diagnostics.Process.Start("https://viewer.autodesk.com"); } catch { }

            TaskDlg.Show("Vista Compartida",
                "Para compartir el modelo online sin exportar:\n\n" +
                "  Revit > Colaborar > Vistas Compartidas\n\n" +
                "Genera un link directo que el equipo de obra\n" +
                "puede abrir desde cualquier navegador.\n\n" +
                "No requiere exportar ni subir archivos.");
            return Result.Succeeded;
        }
    }

    // ════════════════════════════════════════════════════════
    //  FILTROS DE SELECCIÓN CAD / LOSA
    // ════════════════════════════════════════════════════════
    internal class FiltroLosa : ISelectionFilter
    {
        public bool AllowElement(Element elem) => elem is Floor;
        public bool AllowReference(Reference reference, XYZ position) => false;
    }

    internal class FiltroCAD : ISelectionFilter
    {
        public bool AllowElement(Element elem) => elem is ImportInstance;
        public bool AllowReference(Reference reference, XYZ position) => false;
    }

    // ════════════════════════════════════════════════════════
    //  CMD 5 — COLOCAR CASETONES
    // ════════════════════════════════════════════════════════
    [Transaction(TransactionMode.Manual)]
    public class CmdColocarCasetones : IExternalCommand
    {
        public Result Execute(ExternalCommandData data, ref string msg, ElementSet es)
        {
            var uidoc = data.Application.ActiveUIDocument;
            var doc   = uidoc.Document;

            // Listar todas las familias de simbolos
            var listaFamilias = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>()
                .OrderBy(x => x.FamilyName).ThenBy(x => x.Name).ToList();

            if (listaFamilias.Count == 0)
            { TaskDlg.Show("Sin familias", "No hay familias cargadas en el proyecto."); return Result.Cancelled; }

            try
            {
                TaskDlg.Show("Instrucciones",
                    "PASO 1: Selecciona la LOSA que vas a cortar.\n" +
                    "PASO 2: Selecciona el archivo CAD con la reticula.");

                Reference refLosa = uidoc.Selection.PickObject(ObjectType.Element, new FiltroLosa(), "PASO 1: Selecciona la Losa");
                Floor losa = doc.GetElement(refLosa) as Floor;

                Reference refCAD = uidoc.Selection.PickObject(ObjectType.Element, new FiltroCAD(), "PASO 2: Selecciona el archivo CAD");
                ImportInstance cadInstance = doc.GetElement(refCAD) as ImportInstance;

                // Leer capas del CAD
                var capasCAD = new List<string>();
                if (cadInstance.Category != null)
                {
                    foreach (Category sub in cadInstance.Category.SubCategories) capasCAD.Add(sub.Name);
                }
                if (capasCAD.Count == 0) capasCAD.Add("0"); // Fallback

                string nombreFamilia;
                string layerCasetones;

                using (var dlg = new VentanaCasetones(listaFamilias, capasCAD))
                {
                    if (dlg.ShowDialog() != DialogResult.OK) return Result.Cancelled;
                    nombreFamilia  = dlg.FamiliaSeleccionada;
                    layerCasetones = dlg.LayerSeleccionado;
                }

                var simbolo = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>()
                    .FirstOrDefault(x => (x.FamilyName + " : " + x.Name) == nombreFamilia);
                if (simbolo == null)
                { TaskDlg.Show("Error", "No se encontro la familia seleccionada."); return Result.Failed; }

                if (!simbolo.IsActive) { using (var t = new Transaction(doc, "Activar Familia")) { t.Start(); simbolo.Activate(); t.Commit(); } }

                // Obtener solido de la losa para filtrar por desniveles
                Solid solidoLosa = Utils.ObtenerSolidos(losa).OrderByDescending(s => s.Volume).FirstOrDefault();
                double zInferior = losa.get_BoundingBox(null).Min.Z;
                Level nivelLosa = doc.GetElement(losa.LevelId) as Level;

                // Encontrar la cara superior más grande para filtrar por área
                Face caraSuperior = null;
                if (solidoLosa != null)
                {
                    double maxArea = 0;
                    foreach (Face f in solidoLosa.Faces)
                    {
                        XYZ normal = f.ComputeNormal(new UV(0.5, 0.5));
                        if (Math.Abs(normal.Z - 1.0) < 0.01) // Apunta hacia arriba (+Z)
                        {
                            if (f.Area > maxArea) { caraSuperior = f; maxArea = f.Area; }
                        }
                    }
                }

                var todosLosGrupos = new List<List<XYZ>>();
                var lineasSueltas  = new List<Line>();

                ExtraerGeometriaDelCAD(cadInstance.get_Geometry(new Options()), doc, layerCasetones, todosLosGrupos, lineasSueltas);

                if (lineasSueltas.Count > 0)
                {
                    var gruposSueltos = AgruparLineasPorProximidad(lineasSueltas, 0.15);
                    foreach (var grupo in gruposSueltos)
                    {
                        var puntosGrupo = new List<XYZ>();
                        foreach (Line l in grupo) { puntosGrupo.Add(l.GetEndPoint(0)); puntosGrupo.Add(l.GetEndPoint(1)); }
                        todosLosGrupos.Add(puntosGrupo);
                    }
                }

                int casetonesOK = 0, cortesOK = 0, ignorados = 0, fueraLosa = 0;
                var centrosColocados = new List<XYZ>();
                bool esAdaptativo = AdaptiveComponentInstanceUtils.IsAdaptiveFamilySymbol(simbolo);

                using (var t = new Transaction(doc, "Colocacion de Casetones"))
                {
                    t.Start();
                    if (!simbolo.IsActive) simbolo.Activate();

                    foreach (var puntosBrutos in todosLosGrupos)
                    {
                        try
                        {
                            XYZ centro = null;
                            double anchoReal = 0, largoReal = 0;
                            XYZ p1 = null, p2 = null, p3 = null, p4 = null;

                            if (esAdaptativo)
                            {
                                var esquinas = ObtenerEsquinasUnicas(puntosBrutos, 0.15);
                                if (esquinas.Count != 4) { ignorados++; continue; }
                                esquinas = OrdenarPuntosCirculo(esquinas);

                                p1 = new XYZ(esquinas[0].X, esquinas[0].Y, zInferior);
                                p2 = new XYZ(esquinas[1].X, esquinas[1].Y, zInferior);
                                p3 = new XYZ(esquinas[2].X, esquinas[2].Y, zInferior);
                                p4 = new XYZ(esquinas[3].X, esquinas[3].Y, zInferior);

                                centro = new XYZ((p1.X + p3.X) / 2, (p1.Y + p3.Y) / 2, zInferior);
                            }
                            else
                            {
                                // Lógica directa del macro para paramétricos
                                double minX = puntosBrutos.Min(p => p.X);
                                double maxX = puntosBrutos.Max(p => p.X);
                                double minY = puntosBrutos.Min(p => p.Y);
                                double maxY = puntosBrutos.Max(p => p.Y);

                                double cx = (minX + maxX) / 2.0;
                                double cy = (minY + maxY) / 2.0;
                                centro = new XYZ(cx, cy, zInferior);

                                double calcAncho = maxX - minX;
                                double calcLargo = maxY - minY;
                                anchoReal = Math.Min(calcAncho, calcLargo);
                                largoReal = Math.Max(calcAncho, calcLargo);

                                if (anchoReal < 0.3 || largoReal < 0.3) continue; // Filtro de tamaño del macro
                            }

                            // ── FILTRO DE ÁREA (PROYECCIÓN) ──
                            if (caraSuperior != null)
                            {
                                var proj = caraSuperior.Project(centro);
                                if (proj == null) { fueraLosa++; continue; }
                            }

                            // ── FILTRO ANTI-DUPLICADOS ──
                            if (centrosColocados.Any(c => c.DistanceTo(centro) < 0.16)) continue;

                            centrosColocados.Add(centro);
                            FamilyInstance caseton = null;

                            if (esAdaptativo)
                            {
                                caseton = AdaptiveComponentInstanceUtils.CreateAdaptiveComponentInstance(doc, simbolo);
                                IList<ElementId> ptIds = AdaptiveComponentInstanceUtils.GetInstancePlacementPointElementRefIds(caseton);
                                if (ptIds.Count == 4)
                                {
                                    (doc.GetElement(ptIds[0]) as ReferencePoint).Position = p1;
                                    (doc.GetElement(ptIds[1]) as ReferencePoint).Position = p2;
                                    (doc.GetElement(ptIds[2]) as ReferencePoint).Position = p3;
                                    (doc.GetElement(ptIds[3]) as ReferencePoint).Position = p4;
                                    casetonesOK++;
                                }
                            }
                            else
                            {
                                caseton = doc.Create.NewFamilyInstance(centro, simbolo, losa, nivelLosa, StructuralType.NonStructural);
                                casetonesOK++;

                                try
                                {
                                    Parameter pAncho = caseton.LookupParameter("Ancho");
                                    Parameter pLargo = caseton.LookupParameter("Largo");
                                    if (pAncho != null) pAncho.Set(anchoReal);
                                    if (pLargo != null) pLargo.Set(largoReal);
                                }
                                catch { }
                            }

                            try { if (caseton != null) { InstanceVoidCutUtils.AddInstanceVoidCut(doc, losa, caseton); cortesOK++; } } catch { }
                        }
                        catch { }
                    }
                    t.Commit();
                }

                TaskDlg.Show("Casetones",
                    "Proceso terminado.\n\n" +
                    "\u2714 " + casetonesOK + " casetones colocados\n" +
                    "\u2714 " + cortesOK + " cortes en losa\n" +
                    "\u26A0 " + ignorados + " figuras ignoradas (no son 4 lados)\n" +
                    "\u26A0 " + fueraLosa + " descartados (fuera de la losa / desnivel)");
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException) { }
            return Result.Succeeded;
        }

        // ── Funciones internas ──

        /// Verifica si un punto esta dentro de un solido usando ray-casting vertical
        static bool PuntoEnSolido(Solid solid, XYZ punto)
        {
            try
            {
                var bb = solid.GetBoundingBox();
                // Rayo vertical desde muy abajo hasta muy arriba pasando por el punto
                double zMin = bb.Min.Z - 1.0;
                double zMax = bb.Max.Z + 1.0;
                var lineVertical = Line.CreateBound(
                    new XYZ(punto.X, punto.Y, zMin),
                    new XYZ(punto.X, punto.Y, zMax));

                var opts = new SolidCurveIntersectionOptions();
                var result = solid.IntersectWithCurve(lineVertical, opts);

                // El punto esta dentro si cae entre al menos un par de intersecciones
                for (int i = 0; i < result.SegmentCount; i++)
                {
                    var seg = result.GetCurveSegment(i);
                    double segZmin = Math.Min(seg.GetEndPoint(0).Z, seg.GetEndPoint(1).Z);
                    double segZmax = Math.Max(seg.GetEndPoint(0).Z, seg.GetEndPoint(1).Z);
                    if (punto.Z >= segZmin - 0.01 && punto.Z <= segZmax + 0.01)
                        return true;
                }
            }
            catch { }
            return false;
        }

        List<XYZ> ObtenerEsquinasUnicas(List<XYZ> puntos, double tol)
        {
            var unicos = new List<XYZ>();
            foreach (XYZ p in puntos)
                if (!unicos.Any(u => u.DistanceTo(p) < tol)) unicos.Add(p);
            return unicos;
        }

        List<XYZ> OrdenarPuntosCirculo(List<XYZ> puntos)
        {
            double cx = puntos.Average(p => p.X);
            double cy = puntos.Average(p => p.Y);
            return puntos.OrderBy(p => Math.Atan2(p.Y - cy, p.X - cx)).ToList();
        }

        void ExtraerGeometriaDelCAD(GeometryElement geoElem, Document doc, string targetLayer,
            List<List<XYZ>> gruposDePuntos, List<Line> lineasSueltas)
        {
            if (geoElem == null) return;
            foreach (GeometryObject go in geoElem)
            {
                if (go is Line linea && EsLayerCorrecto(linea, targetLayer, doc))
                    lineasSueltas.Add(linea);
                else if (go is PolyLine poly && EsLayerCorrecto(poly, targetLayer, doc))
                    gruposDePuntos.Add(poly.GetCoordinates().ToList());
                else if (go is GeometryInstance gi)
                    ExtraerGeometriaDelCAD(gi.GetInstanceGeometry(), doc, targetLayer, gruposDePuntos, lineasSueltas);
            }
        }

        bool EsLayerCorrecto(GeometryObject obj, string target, Document doc)
        {
            if (string.IsNullOrEmpty(target)) return true;
            ElementId gsId = obj.GraphicsStyleId;
            if (gsId != ElementId.InvalidElementId)
            {
                var gs = doc.GetElement(gsId) as GraphicsStyle;
                if (gs?.GraphicsStyleCategory != null)
                {
                    string catName = gs.GraphicsStyleCategory.Name;
                    string[] targets = target.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                    return targets.Any(t => catName.Equals(t.Trim(), StringComparison.InvariantCultureIgnoreCase));
                }
            }
            return false;
        }

        List<List<Line>> AgruparLineasPorProximidad(List<Line> lineas, double tolerancia)
        {
            int n = lineas.Count;
            int[] padre = new int[n];
            for (int i = 0; i < n; i++) padre[i] = i;

            int Encontrar(int i) => padre[i] == i ? i : padre[i] = Encontrar(padre[i]);
            void Unir(int a, int b) { int ra = Encontrar(a), rb = Encontrar(b); if (ra != rb) padre[ra] = rb; }

            for (int i = 0; i < n; i++)
                for (int j = i + 1; j < n; j++)
                {
                    XYZ a0 = lineas[i].GetEndPoint(0), a1 = lineas[i].GetEndPoint(1);
                    XYZ b0 = lineas[j].GetEndPoint(0), b1 = lineas[j].GetEndPoint(1);
                    if (a0.DistanceTo(b0) < tolerancia || a0.DistanceTo(b1) < tolerancia ||
                        a1.DistanceTo(b0) < tolerancia || a1.DistanceTo(b1) < tolerancia)
                        Unir(i, j);
                }

            var dic = new Dictionary<int, List<Line>>();
            for (int i = 0; i < n; i++)
            {
                int raiz = Encontrar(i);
                if (!dic.ContainsKey(raiz)) dic[raiz] = new List<Line>();
                dic[raiz].Add(lineas[i]);
            }
            return dic.Values.ToList();
        }
    }

    // ════════════════════════════════════════════════════════
    //  VENTANA CASETONES
    // ════════════════════════════════════════════════════════
    internal class VentanaCasetones : System.Windows.Forms.Form
    {
        public string FamiliaSeleccionada { get; private set; }
        public string LayerSeleccionado   { get; private set; }

        List<FamilySymbol> _adaptativas;
        List<FamilySymbol> _parametricas;
        RadioButton rbAdaptativo, rbParametrico;
        System.Windows.Forms.ComboBox cmb;

        public VentanaCasetones(List<FamilySymbol> familias, List<string> capas)
        {
            _adaptativas = familias.Where(s => AdaptiveComponentInstanceUtils.IsAdaptiveFamilySymbol(s)).ToList();
            _parametricas = familias.Where(s => !AdaptiveComponentInstanceUtils.IsAdaptiveFamilySymbol(s)).ToList();

            Text = "Colocar Casetones"; Size = new WinSize(468, 430);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false;
            BackColor = Esti.FondoOscuro; ForeColor = Esti.TextoPrincipal;
            Controls.Add(Esti.HeaderLogo(468));

            Controls.Add(Esti.Titulo("COLOCAR CASETONES", 16, 80));
            Controls.Add(Esti.Lbl("Coloca familias desde reticula CAD sobre la losa.", 16, 108));
            Controls.Add(Esti.Sep(132));

            // ── Opciones de Tipo ──
            rbAdaptativo = new RadioButton
            {
                Text = "Adaptativos (4 Puntos)", Location = new WinPoint(16, 146), AutoSize = true, Checked = true,
                ForeColor = Esti.TextoPrincipal, Font = Esti.FNormal, BackColor = WinColor.Transparent
            };
            rbParametrico = new RadioButton
            {
                Text = "Paramétricos (1 Punto + Parámetros)", Location = new WinPoint(210, 146), AutoSize = true,
                ForeColor = Esti.TextoPrincipal, Font = Esti.FNormal, BackColor = WinColor.Transparent
            };
            rbAdaptativo.CheckedChanged += (s, e) => ActualizarCombo();
            rbParametrico.CheckedChanged += (s, e) => ActualizarCombo();
            Controls.AddRange(new System.Windows.Forms.Control[] { rbAdaptativo, rbParametrico });

            Controls.Add(Esti.Lbl("Selecciona la Familia:", 16, 182));
            cmb = new System.Windows.Forms.ComboBox
            {
                Location = new WinPoint(16, 204), Size = new WinSize(420, 24),
                DropDownStyle = ComboBoxStyle.DropDownList, Font = Esti.FNormal
            };
            Controls.Add(cmb);

            Controls.Add(Esti.Lbl("Selecciona la capa de AutoCAD:", 16, 242));
            var cmbLayer = new System.Windows.Forms.ComboBox
            {
                Location = new WinPoint(16, 264), Size = new WinSize(420, 24),
                DropDownStyle = ComboBoxStyle.DropDownList, Font = Esti.FNormal
            };
            cmbLayer.Items.AddRange(capas.OrderBy(x => x).ToArray());
            if (cmbLayer.Items.Count > 0) cmbLayer.SelectedIndex = 0;
            Controls.Add(cmbLayer);

            Controls.Add(Esti.Sep(304));
            Controls.Add(Esti.PanelInfo(
                "Solo se colocan casetones en el área de la losa.\n" +
                "Los que caen fuera se filtran automáticamente.",
                314, Esti.ColVerde, 44));

            var bOK  = Esti.Btn("Iniciar", Esti.Acento,  268, 374, 100, 32);
            var bCan = Esti.Btn("Cancelar", Esti.ColGris, 376, 374,  76, 32);
            bOK.Click += (s, e) =>
            {
                FamiliaSeleccionada = cmb.Text;
                LayerSeleccionado   = cmbLayer.Text;
                DialogResult = DialogResult.OK; Close();
            };
            bCan.DialogResult = DialogResult.Cancel;
            Controls.AddRange(new System.Windows.Forms.Control[] { bOK, bCan });
            AcceptButton = bOK; CancelButton = bCan;

            ActualizarCombo();
        }

        private void ActualizarCombo()
        {
            var lista = rbAdaptativo.Checked ? _adaptativas : _parametricas;
            cmb.DataSource = lista.Select(x => x.FamilyName + " : " + x.Name).ToList();
        }
    }
    // ════════════════════════════════════════════════════════
    //  CMD 6 — RENOMBRAR FAMILIAS
    // ════════════════════════════════════════════════════════
    [Transaction(TransactionMode.Manual)]
    public class CmdRenombrarFamilias : IExternalCommand
    {
        public Result Execute(ExternalCommandData data, ref string msg, ElementSet es)
        {
            var uidoc = data.Application.ActiveUIDocument;
            var doc   = uidoc.Document;

            var seleccionIds = uidoc.Selection.GetElementIds();
            var listaFamilias = new List<FamilySymbol>();

            if (seleccionIds.Count > 0)
            {
                foreach (var id in seleccionIds)
                {
                    var elem = doc.GetElement(id);
                    if (elem is FamilyInstance fi) listaFamilias.Add(fi.Symbol);
                    else if (elem is FamilySymbol fs) listaFamilias.Add(fs);
                }
            }

            using (var dlg = new VentanaRenombrar(doc, listaFamilias))
            {
                if (dlg.ShowDialog() != DialogResult.OK) return Result.Cancelled;

                var cambios = dlg.CambiosPlanificados;
                if (cambios.Count == 0) return Result.Succeeded;

                bool aplicarATipo = dlg.AplicarATipo;

                using (var t = new Transaction(doc, "Renombrar Familias"))
                {
                    t.Start();
                    int exito = 0;
                    var familiasProcesadas = new HashSet<ElementId>();

                    foreach (var c in cambios)
                    {
                        try
                        {
                            if (aplicarATipo)
                            {
                                c.Symbol.Name = c.NuevoNombre;
                                exito++;
                            }
                            else
                            {
                                var fId = c.Symbol.Family.Id;
                                if (!familiasProcesadas.Contains(fId))
                                {
                                    c.Symbol.Family.Name = c.NuevoNombre;
                                    familiasProcesadas.Add(fId);
                                    exito++;
                                }
                            }
                        }
                        catch { }
                    }
                    t.Commit();
                    TaskDlg.Show("Renombrar", $"{exito} elementos renombrados.");
                }
            }
            return Result.Succeeded;
        }
    }

    internal class CambioNombre
    {
        public FamilySymbol Symbol { get; set; }
        public string NombreOriginal { get; set; }
        public string NuevoNombre { get; set; }
    }

    internal class VentanaRenombrar : System.Windows.Forms.Form
    {
        public List<CambioNombre> CambiosPlanificados { get; private set; } = new List<CambioNombre>();
        public bool AplicarATipo => cmbAplicarA.SelectedIndex == 0;

        System.Windows.Forms.TextBox txtPrefijo, txtBuscar, txtReemplazar;
        System.Windows.Forms.ComboBox cmbSufijo, cmbCategoria;
        System.Windows.Forms.ComboBox cmbAplicarA;
        RadioButton rbSel, rbTodo;
        System.Windows.Forms.DataGridView dgv;

        Document _doc;
        List<FamilySymbol> _seleccionados;
        List<FamilySymbol> _todasFamilias;

        public VentanaRenombrar(Document doc, List<FamilySymbol> seleccionados)
        {
            _doc = doc;
            _seleccionados = seleccionados;
            _todasFamilias = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>().ToList();

            Text = "Renombrar Familias"; Size = new WinSize(550, 640);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false;
            BackColor = Esti.FondoOscuro; ForeColor = Esti.TextoPrincipal;
            Controls.Add(Esti.HeaderLogo(550));

            Controls.Add(Esti.Titulo("RENOMBRAR FAMILIAS", 16, 80));
            Controls.Add(Esti.Lbl("Agrega prefijos/sufijos y reemplaza texto.", 16, 108));
            Controls.Add(Esti.Sep(132));

            // Alcance
            rbSel = new RadioButton { Text = $"Selección ({_seleccionados.Count})", Location = new WinPoint(16, 142), AutoSize = true, Checked = _seleccionados.Count > 0, ForeColor = Esti.TextoPrincipal, Font = Esti.FNormal, Enabled = _seleccionados.Count > 0 };
            rbTodo = new RadioButton { Text = "Todo el proyecto", Location = new WinPoint(200, 142), AutoSize = true, Checked = _seleccionados.Count == 0, ForeColor = Esti.TextoPrincipal, Font = Esti.FNormal };
            Controls.AddRange(new System.Windows.Forms.Control[] { rbSel, rbTodo });

            // Aplicar a
            Controls.Add(Esti.Lbl("Aplicar cambio a:", 16, 172));
            cmbAplicarA = new System.Windows.Forms.ComboBox { Location = new WinPoint(16, 194), Size = new WinSize(518, 24), DropDownStyle = ComboBoxStyle.DropDownList, Font = Esti.FNormal };
            cmbAplicarA.Items.AddRange(new string[] { "Nombre del Tipo (Type)", "Nombre de la Familia (Family)" });
            cmbAplicarA.SelectedIndex = 0;
            Controls.Add(cmbAplicarA);

            // Filtro Categoría
            Controls.Add(Esti.Lbl("Filtrar Categoría:", 16, 224));
            cmbCategoria = new System.Windows.Forms.ComboBox { Location = new WinPoint(16, 246), Size = new WinSize(518, 24), DropDownStyle = ComboBoxStyle.DropDownList, Font = Esti.FNormal };
            CargarCategorias();
            Controls.Add(cmbCategoria);

            // Operaciones
            int yOp = 282;
            Controls.Add(Esti.Lbl("Prefijo:", 16, yOp));
            txtPrefijo = new System.Windows.Forms.TextBox { Location = new WinPoint(16, yOp + 22), Size = new WinSize(250, 24), Font = Esti.FNormal };

            Controls.Add(Esti.Lbl("Sufijo / Unidad:", 284, yOp));
            cmbSufijo = new System.Windows.Forms.ComboBox { Location = new WinPoint(284, yOp + 22), Size = new WinSize(250, 24), Font = Esti.FNormal };
            cmbSufijo.Items.AddRange(new string[] { "", "_m", "_cm", "_kg", "_mm", "_lb" });

            int yRe = yOp + 58;
            Controls.Add(Esti.Lbl("Buscar:", 16, yRe));
            txtBuscar = new System.Windows.Forms.TextBox { Location = new WinPoint(16, yRe + 22), Size = new WinSize(250, 24), Font = Esti.FNormal };

            Controls.Add(Esti.Lbl("Reemplazar con:", 284, yRe));
            txtReemplazar = new System.Windows.Forms.TextBox { Location = new WinPoint(284, yRe + 22), Size = new WinSize(250, 24), Font = Esti.FNormal };

            Controls.AddRange(new System.Windows.Forms.Control[] { txtPrefijo, cmbSufijo, txtBuscar, txtReemplazar });

            // Eventos
            txtPrefijo.TextChanged += Updates;
            cmbSufijo.TextChanged += Updates;
            txtBuscar.TextChanged += Updates;
            txtReemplazar.TextChanged += Updates;
            rbSel.CheckedChanged += Updates;
            rbTodo.CheckedChanged += Updates;
            cmbCategoria.SelectedIndexChanged += Updates;
            cmbAplicarA.SelectedIndexChanged += Updates;

            // DataGridView
            dgv = new System.Windows.Forms.DataGridView
            {
                Location = new WinPoint(16, yRe + 56), Size = new WinSize(518, 160),
                BackgroundColor = Esti.FondoPanel, BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                ReadOnly = true, RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgv.Columns.Add("Original", "Original");
            dgv.Columns.Add("Nuevo", "Nuevo");
            Controls.Add(dgv);

            var bOK = Esti.Btn("Renombrar", Esti.Acento, 340, yRe + 226, 120, 32);
            var bCan = Esti.Btn("Cancelar", Esti.ColGris, 468, yRe + 226, 68, 32);
            bOK.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };
            bCan.DialogResult = DialogResult.Cancel;
            Controls.AddRange(new System.Windows.Forms.Control[] { bOK, bCan });
            AcceptButton = bOK; CancelButton = bCan;

            ActualizarPreview();
        }

        void CargarCategorias()
        {
            cmbCategoria.Items.Add("Todas");
            var cats = _todasFamilias.Select(f => f.Category?.Name).Distinct().Where(n => n != null).OrderBy(n => n).ToList();
            cmbCategoria.Items.AddRange(cats.ToArray());
            cmbCategoria.SelectedIndex = 0;
        }

        void Updates(object sender, EventArgs e) => ActualizarPreview();

        void ActualizarPreview()
        {
            CambiosPlanificados.Clear();
            dgv.Rows.Clear();

            var listaBase = rbSel.Checked ? _seleccionados : _todasFamilias;
            string catFiltrada = cmbCategoria.Text;

            var filtrados = listaBase.Where(f => catFiltrada == "Todas" || (f.Category?.Name == catFiltrada)).ToList();

            string pref = txtPrefijo.Text;
            string suf = cmbSufijo.Text;
            string buscar = txtBuscar.Text;
            string reemp = txtReemplazar.Text;
            bool calcTipo = AplicarATipo;

            var nombresUnicos = new HashSet<string>();

            foreach (var fs in filtrados)
            {
                string original = calcTipo ? fs.Name : fs.Family.Name;
                if (!calcTipo && nombresUnicos.Contains(original)) continue; // Evitar duplicar filas para familias en el previsualizador

                string nuevo = original;
                if (!string.IsNullOrEmpty(buscar)) nuevo = nuevo.Replace(buscar, reemp);
                nuevo = pref + nuevo + suf;

                if (nuevo != original)
                {
                    if (!calcTipo) nombresUnicos.Add(original);
                    CambiosPlanificados.Add(new CambioNombre { Symbol = fs, NombreOriginal = original, NuevoNombre = nuevo });
                    dgv.Rows.Add(original, nuevo);
                }
            }
        }
    }
}
