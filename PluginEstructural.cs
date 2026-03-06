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
using Autodesk.Revit.UI;

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
            foreach (Parameter p in e.Parameters)
                try { if (p.HasValue && !p.IsReadOnly) sb.Append(p.AsValueString() ?? ""); } catch { }
            return sb.ToString().GetHashCode().ToString("X8");
        }

        internal static Dictionary<string, string> ParamsLegibles(Element e)
        {
            var dic = new Dictionary<string, string>();
            var nombres = new[] { "b", "h", "Ancho", "Alto", "Espesor", "Longitud",
                "Desfase base", "Desfase superior", "Tipo" };
            foreach (var n in nombres)
            {
                var p = e.LookupParameter(n);
                if (p != null && p.HasValue)
                    dic[n] = p.AsValueString() ?? p.AsString() ?? "";
            }
            var bb = e.get_BoundingBox(null);
            if (bb != null) dic["Pos Z"] = bb.Min.Z.ToString("F2") + "m";

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
    }

    // ════════════════════════════════════════════════════════
    //  RIBBON
    // ════════════════════════════════════════════════════════
    public class AppInicio : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication app)
        {
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
                        doc.Export(dlg.RutaExportacion, Utils.Safe(doc.Title) + "_SAT", new List<ElementId> { vista.Id }, opts);
                        TaskDlg.Show("SAT Listo",
                            "Solidos: " + sols.Count + " | Shapes: " + gen.Count +
                            "\n\nSe ha exportado el archivo SAT en:\n" + dlg.RutaExportacion +
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
                    return Comparar(doc, uidoc, reg, old);
                }
                Limpiar(doc);
                TaskDlg.Show("Listo", "Colores eliminados.");
            }
            return Result.Succeeded;
        }

        string BuscarOLD(string reg)
        {
            if (!Directory.Exists(reg)) return null;
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

        Result Comparar(Document doc, UIDocument uidoc, string reg, string oldDir)
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
            var lista = new List<(string estado, string desc, string cat, string tipo, string uid, string famTipo, string nivel)>();
            foreach (var e in nuevos)
            {
                var pd = HashElem.ParamsLegibles(e);
                string ft = pd.ContainsKey("Familia y Tipo") ? pd["Familia y Tipo"] : "";
                string nv = pd.ContainsKey("Nivel / Restriccion") ? pd["Nivel / Restriccion"] : "";
                lista.Add(("NUEVO", Gemini.NuevoDesc(e.Category?.Name ?? "", ft),
                    e.Category?.Name ?? "", e.Name, e.UniqueId, ft, nv));
            }
            foreach (var e in mods)
            {
                var pa = ant.ContainsKey(e.UniqueId) ? ant[e.UniqueId].pars : new Dictionary<string, string>();
                var pd = HashElem.ParamsLegibles(e);
                string ft = pd.ContainsKey("Familia y Tipo") ? pd["Familia y Tipo"] : "";
                string nv = pd.ContainsKey("Nivel / Restriccion") ? pd["Nivel / Restriccion"] : "";
                lista.Add(("MODIFICADO",
                    Gemini.Describir(e.Category?.Name ?? "", e.Name, pa, pd, nv, ft),
                    e.Category?.Name ?? "", e.Name, e.UniqueId, ft, nv));
            }
            foreach (var uid in elims)
            {
                var pa = ant[uid].pars;
                string ft = pa.ContainsKey("Familia y Tipo") ? pa["Familia y Tipo"] : ant[uid].cat + " - " + ant[uid].tipo;
                string nv = pa.ContainsKey("Nivel / Restriccion") ? pa["Nivel / Restriccion"] : "Sin nivel";
                lista.Add(("ELIMINADO", Gemini.ElimDesc(ant[uid].cat, ft),
                    ant[uid].cat, ant[uid].tipo, uid, ft, nv));
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
            List<(string estado, string desc, string cat, string tipo, string uid, string famTipo, string nivel)> lista)
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
                        var pic = ws.AddPicture(logoPath).MoveTo(ws.Cell(1, 1), 15, 10);
                        pic.Width = 160; pic.Height = 45;
                    }

                    ws.Range("D1:F3").Style.Fill.BackgroundColor = cBlanco;
                    ws.Range("D1:F1").Merge(); ws.Range("D2:F2").Merge(); ws.Range("D3:F3").Merge();
                    
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
                    // 8 columnas: N° | Estado | Descripción del Cambio | Categoría | Familia y Tipo | Nivel | Ver en Viewer | UniqueId
                    var hdrs = new[] { "N°", "Estado", "Descripción del Cambio", "Categoría",
                        "Familia y Tipo", "Nivel / Restricción", "Ver en Viewer", "UniqueId" };
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

                            // Columna 7: link de Viewer — vacia para que el usuario la llene manualmente
                            ws.Cell(fila, 7).Value = "";
                            ws.Cell(fila, 7).Style.Fill.BackgroundColor = cGrisClar;
                            ws.Cell(fila, 7).Style.Border.BottomBorder = ClosedXML.Excel.XLBorderStyleValues.Hair;
                            ws.Cell(fila, 7).Style.Border.BottomBorderColor = cBorde;

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

                    // ── HOJA 2: AUDITORIA ────────────────────────────────────
                    var ws2 = wb.Worksheets.Add("AUDITORIA");
                    ws2.ShowGridLines = false;
                    var hdrs2 = new[] { "Estado", "UniqueId", "Categoria", "Familia y Tipo", "Nivel" };
                    for (int c = 1; c <= 5; c++)
                    {
                        ws2.Cell(1, c).Value = hdrs2[c - 1];
                        ws2.Cell(1, c).Style.Font.Bold = true;
                        ws2.Cell(1, c).Style.Fill.BackgroundColor = cGrisOsc;
                        ws2.Cell(1, c).Style.Font.FontColor = cBlanco;
                        ws2.Cell(1, c).Style.Font.FontSize = 10;
                    }
                    ws2.Row(1).Height = 20;

                    int f2 = 2;
                    foreach (var item in lista)
                    {
                        bool par2 = (f2 % 2 == 0);
                        ws2.Cell(f2, 1).Value = item.estado;
                        ws2.Cell(f2, 1).Style.Font.Bold = true;
                        if (item.estado == "NUEVO")           { ws2.Cell(f2, 1).Style.Fill.BackgroundColor = cBgNuevo; ws2.Cell(f2, 1).Style.Font.FontColor = cTxNuevo; }
                        else if (item.estado == "MODIFICADO") { ws2.Cell(f2, 1).Style.Fill.BackgroundColor = cBgMod;   ws2.Cell(f2, 1).Style.Font.FontColor = cTxMod;   }
                        else                                   { ws2.Cell(f2, 1).Style.Fill.BackgroundColor = cBgElim;  ws2.Cell(f2, 1).Style.Font.FontColor = cTxElim;  }
                        ws2.Cell(f2, 2).Value = item.uid;
                        ws2.Cell(f2, 3).Value = item.cat;
                        ws2.Cell(f2, 4).Value = item.famTipo;
                        ws2.Cell(f2, 5).Value = item.nivel;
                        var rg = ws2.Range(f2, 2, f2, 5);
                        rg.Style.Fill.BackgroundColor = par2 ? cBlanco : cGrisClar;
                        rg.Style.Font.FontColor = cTextoGris;
                        rg.Style.Border.BottomBorder = ClosedXML.Excel.XLBorderStyleValues.Hair;
                        rg.Style.Border.BottomBorderColor = cBorde;
                        f2++;
                    }
                    ws2.Columns().AdjustToContents();
                    ws2.SheetView.FreezeRows(1);

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
        RadioButton rbSel, rbTodo, rbNiv;
        CheckedListBox clb;

        // Persistir ultima ruta de exportacion durante la sesion de Revit
        static string _ultimaRuta = null;

        public VentanaSAT(Document doc)
        {
            Text = "Exportar SAT"; Size = new WinSize(468, 580);
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

            Controls.Add(Esti.Lbl("Alcance:", 16, 202));
            rbSel  = RB("Usar seleccion activa", 16, 224, true);
            rbTodo = RB("Todo el modelo estructural", 16, 248);
            rbNiv  = RB("Filtrar por niveles:", 16, 272);
            rbNiv.CheckedChanged += (s, e) => clb.Enabled = rbNiv.Checked;
            clb = new CheckedListBox
            {
                Location = new WinPoint(32, 296), Size = new WinSize(400, 130),
                Enabled = false, BackColor = Esti.FondoMedio,
                ForeColor = Esti.TextoPrincipal, Font = Esti.FNormal, BorderStyle = BorderStyle.None
            };
            foreach (Level lv in new FilteredElementCollector(doc).OfClass(typeof(Level))
                .Cast<Level>().OrderBy(l => l.Elevation))
                clb.Items.Add(lv.Name);
            Controls.Add(Esti.Sep(436));
            Controls.Add(Esti.PanelInfo(
                "Exportacion SAT automatica a la ruta indicada tras generar vista.",
                446, Esti.Acento, 40));
            var bOK  = Esti.Btn("Exportar", Esti.Acento,  244, 496, 130, 32);
            var bCan = Esti.Btn("Cancelar", Esti.ColGris, 382, 496,  72, 32);
            bOK.Click += (s, e) =>
            {
                UsarSeleccion = rbSel.Checked;
                if (rbNiv.Checked) Niveles = clb.CheckedItems.Cast<string>().ToList();
                RutaExportacion = txtRuta.Text;
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

        // Ultima ruta usada en la sesion de Revit
        static string _ultimaRuta = null;

        public VentanaComparar(string regActual, Func<string, string> funcBuscarOLD)
        {
            // Si hay una ruta persistida, usarla como punto de partida
            string ruta = _ultimaRuta ?? regActual;
            Text = "Comparar Versiones"; Size = new WinSize(468, 540);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false;
            BackColor = Esti.FondoOscuro; ForeColor = Esti.TextoPrincipal;
            Controls.Add(Esti.HeaderLogo(468));
            Controls.Add(Esti.Titulo("COMPARADOR DE VERSIONES", 16, 80));
            Controls.Add(Esti.Lbl("Registro OLD/NEW con reporte Gemini AI y vista limpia.", 16, 108));
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
            
            var bG = Esti.BtnGrande("Guardar version OLD\n    Exporta IFC + estado actual como linea base", 270);
            var bC = Esti.BtnGrande(
                "Comparar con OLD y generar NEW\n    Colorea + vista limpia + reporte IA + auditoria",
                334, false, Esti.FondoPanel);
            var bL = Esti.BtnGrande("Limpiar colores de la vista activa", 398, true, Esti.ColGris);

            Action actualizarEstado = () => {
                string rOld = funcBuscarOLD(txtRuta.Text);
                bool hay = (rOld != null);
                lblEst.Text = hay ? "Version OLD encontrada: " + Path.GetFileName(Path.GetDirectoryName(rOld))
                                  : "Sin version OLD — guarda una antes de comparar";
                lblEst.ForeColor = hay ? Esti.ColVerde : Esti.ColAmarillo;
                bC.Enabled = hay;
                bC.FlatAppearance.BorderColor = hay ? Esti.Acento : Esti.ColGris;
                bC.ForeColor = hay ? Esti.TextoPrincipal : Esti.TextoSecundario;
            };

            actualizarEstado();

            btnRuta.Click += (s, e) => {
                using (var fbd = new FolderBrowserDialog { Description = "Carpeta de Registro BIM", SelectedPath = txtRuta.Text }) {
                    if (fbd.ShowDialog() == DialogResult.OK) {
                        txtRuta.Text = fbd.SelectedPath;
                        RutaElegida = fbd.SelectedPath;
                        _ultimaRuta = fbd.SelectedPath;   // persistir para la proxima vez
                        actualizarEstado();
                    }
                }
            };

            bG.Click += (s, e) => { _ultimaRuta = RutaElegida; Op = AccionComp.GuardarOLD;       DialogResult = DialogResult.OK; Close(); };
            bC.Click += (s, e) => { _ultimaRuta = RutaElegida; Op = AccionComp.CompararYGenerar; DialogResult = DialogResult.OK; Close(); };
            bL.Click += (s, e) => { _ultimaRuta = RutaElegida; Op = AccionComp.Limpiar;          DialogResult = DialogResult.OK; Close(); };
            Controls.Add(Esti.Sep(460));
            var bCan = Esti.Btn("Cancelar", Esti.ColGris, 384, 468, 70, 28);
            bCan.DialogResult = DialogResult.Cancel;
            Controls.AddRange(new System.Windows.Forms.Control[] { bG, bC, bL, bCan });
            CancelButton = bCan;
        }
    }

    // ════════════════════════════════════════════════════════
    //  CMD 3 — CORTE JERARQUICO
    // ════════════════════════════════════════════════════════
    internal enum AlcanceCorte { VistaActiva, TodoElModelo }

    [Transaction(TransactionMode.Manual)]
    public class CmdCorteJerarquico : IExternalCommand
    {
        // Jerarquia: mayor prioridad corta a menor.
        // 0=Suelos/Losas, 1=Pilares/Columnas, 2=Vigas/Armazones,
        // 3=Muros, 4=Cubiertas, 5=Cimentaciones, 6=Escaleras/Rampas/Varios
        static readonly List<BuiltInCategory>[] Jerarquia = new[]
        {
            new List<BuiltInCategory> { BuiltInCategory.OST_Floors },
            new List<BuiltInCategory> { BuiltInCategory.OST_StructuralColumns, BuiltInCategory.OST_Columns },
            new List<BuiltInCategory> { BuiltInCategory.OST_StructuralFraming, BuiltInCategory.OST_StructuralTruss },
            new List<BuiltInCategory> { BuiltInCategory.OST_Walls },
            new List<BuiltInCategory> { BuiltInCategory.OST_Roofs },
            new List<BuiltInCategory> { BuiltInCategory.OST_StructuralFoundation, BuiltInCategory.OST_StructConnections },
            new List<BuiltInCategory> { BuiltInCategory.OST_Stairs, BuiltInCategory.OST_Ramps, BuiltInCategory.OST_GenericModel },
        };
        static readonly string[] NombresNivel =
        {
            "Suelos / Losas",
            "Pilares / Columnas",
            "Vigas / Armazones",
            "Muros",
            "Cubiertas",
            "Cimentaciones / Conexiones",
            "Escaleras / Rampas / Varios",
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
                    var col = alcance == AlcanceCorte.VistaActiva
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
                    "Alcance: " + (alcance == AlcanceCorte.VistaActiva ? "Vista activa" : "Todo el modelo"));
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
            "Escaleras / Rampas / Varios",
        };

        public bool[]       NivelesActivos { get; private set; } = new bool[7];
        public AlcanceCorte Alcance        { get; private set; } = AlcanceCorte.VistaActiva;
        public bool         AutoSwitch     { get; private set; } = true;

        RadioButton rbVista, rbTodo;
        CheckBox[]  chks = new CheckBox[7];
        CheckBox    chkSwitch;

        public VentanaCorte()
        {
            Text = "Corte Geometrico"; Size = new WinSize(468, 660);
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
            rbVista = new RadioButton { Text = "Solo vista activa (mas rapido)",
                Location = new WinPoint(16, 164), AutoSize = true, Checked = true,
                ForeColor = Esti.TextoPrincipal, Font = Esti.FNormal, BackColor = WinColor.Transparent };
            rbTodo  = new RadioButton { Text = "Todo el modelo",
                Location = new WinPoint(16, 188), AutoSize = true,
                ForeColor = Esti.TextoPrincipal, Font = Esti.FNormal, BackColor = WinColor.Transparent };
            Controls.AddRange(new System.Windows.Forms.Control[] { rbVista, rbTodo });
            Controls.Add(Esti.Sep(212));

            // ── Niveles jerarquicos ────────────────────────────
            Controls.Add(Esti.Lbl("Niveles a procesar (de mayor a menor jerarquia):", 16, 224));
            // Columores para cada nivel (arcoiris suave)
            WinColor[] colores = {
                WinColor.FromArgb(22,160,230),  // azul  — Suelos
                WinColor.FromArgb(39,201,111),  // verde — Pilares
                WinColor.FromArgb(255,196,0),   // amarillo — Vigas
                WinColor.FromArgb(255,138,60),  // naranja — Muros
                WinColor.FromArgb(229,57,53),   // rojo — Cubiertas
                WinColor.FromArgb(156,39,176),  // morado — Cimentacion
                WinColor.FromArgb(100,110,120), // gris — Varios
            };
            for (int i = 0; i < 7; i++)
            {
                int y = 248 + i * 30;
                // Indicador de color de nivel
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
                Location = new WinPoint(16, 462), Size = new WinSize(70, 24),
                Font = Esti.FPeque, BackColor = Esti.Acento, ForeColor = WinColor.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            bAll.FlatAppearance.BorderSize = 0;
            bAll.Click += (s, e) => { foreach (var c in chks) c.Checked = true; };
            var bNone = new WinButton { Text = "Ninguno",
                Location = new WinPoint(92, 462), Size = new WinSize(70, 24),
                Font = Esti.FPeque, BackColor = Esti.ColGris, ForeColor = WinColor.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            bNone.FlatAppearance.BorderSize = 0;
            bNone.Click += (s, e) => { foreach (var c in chks) c.Checked = false; };
            Controls.AddRange(new System.Windows.Forms.Control[] { bAll, bNone });

            Controls.Add(Esti.Sep(492));

            // ── Opciones ───────────────────────────────────────
            chkSwitch = new CheckBox { Text = "Corregir orden de corte automaticamente (SwitchJoinOrder)",
                Location = new WinPoint(16, 504), AutoSize = true, Checked = true,
                ForeColor = Esti.TextoSecundario, Font = Esti.FPeque, BackColor = WinColor.Transparent };
            Controls.Add(chkSwitch);

            // Info
            Controls.Add(Esti.PanelInfo(
                "El elemento de mayor jerarquia corta al de menor. Usa BoundingBox para optimizar rendimiento.",
                524, Esti.Acento, 36));

            // ── Botones ────────────────────────────────────────
            var bOK  = Esti.Btn("Ejecutar", Esti.Acento,  230, 568, 130, 32);
            var bCan = Esti.Btn("Cancelar", Esti.ColGris, 368, 568,  82, 32);
            bOK.Click += (s, e) =>
            {
                Alcance    = rbVista.Checked ? AlcanceCorte.VistaActiva : AlcanceCorte.TodoElModelo;
                AutoSwitch = chkSwitch.Checked;
                for (int i = 0; i < 7; i++) NivelesActivos[i] = chks[i].Checked;
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
}
