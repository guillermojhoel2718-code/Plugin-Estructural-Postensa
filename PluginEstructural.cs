using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using RevitView  = Autodesk.Revit.DB.View;
using RevitPoint = Autodesk.Revit.DB.XYZ;
using WinPoint   = System.Drawing.Point;
using WinSize    = System.Drawing.Size;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using WinLabel  = System.Windows.Forms.Label;
using WinPanel  = System.Windows.Forms.Panel;
using WinColor  = System.Drawing.Color;
using RevColor  = Autodesk.Revit.DB.Color;

using TaskDlg   = Autodesk.Revit.UI.TaskDialog;

namespace PluginEstructural
{
    // ═══════════════════════════════════════════════════════
    //  RIBBON
    // ═══════════════════════════════════════════════════════
    // Métodos requeridos por ThisApplication.Designer.cs de Revit Macros
    public partial class ThisApplication
    {
        private void Module_Startup(object sender, EventArgs e) { }
        private void Module_Shutdown(object sender, EventArgs e) { }
    }

    public class AppInicio : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication app)
        {
            try
            {
                const string TAB = "BIM Estructural";
                try { app.CreateRibbonTab(TAB); } catch { }
                RibbonPanel panel = app.CreateRibbonPanel(TAB, "Herramientas BIM");
                string dll = typeof(AppInicio).Assembly.Location;

                panel.AddItem(new PushButtonData("ExportarSAT",      "Exportar\nSAT",
                    dll, "PluginEstructural.CmdExportarSAT")
                    { ToolTip = "Genera sólido único -90° para SolidWorks (.SAT)" });
                panel.AddSeparator();
                panel.AddItem(new PushButtonData("CompararVersiones", "Comparar\nVersiones",
                    dll, "PluginEstructural.CmdCompararVersiones")
                    { ToolTip = "Exporta IFC OLD/NEW y colorea diferencias — registro de auditoría" });
                panel.AddSeparator();
                panel.AddItem(new PushButtonData("GenerarViewer",     "Generar\nViewer",
                    dll, "PluginEstructural.CmdGenerarViewer")
                    { ToolTip = "Exporta IFC con colores y genera viewer HTML para obra" });

                return Result.Succeeded;
            }
            catch (Exception ex) { TaskDlg.Show("Error plugin", ex.Message); return Result.Failed; }
        }
        public Result OnShutdown(UIControlledApplication app) => Result.Succeeded;
    }

    // ═══════════════════════════════════════════════════════
    //  UTILIDADES COMPARTIDAS
    // ═══════════════════════════════════════════════════════
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
            (long)BuiltInCategory.OST_StructuralFraming, (long)BuiltInCategory.OST_StructuralTruss,
            (long)BuiltInCategory.OST_StructConnections, (long)BuiltInCategory.OST_StructuralStiffener,
        };

        internal static bool EsPermitido(Element e) =>
            e?.Category != null && Cats.Any(c => (long)c == e.Category.Id.Value);

        internal static List<Solid> ObtenerSolidos(Element elem)
        {
            var lst  = new List<Solid>();
            bool ac  = elem.Category != null && CatsAcero.Contains(elem.Category.Id.Value);
            ProcGeo(elem.get_Geometry(new Options { DetailLevel = ViewDetailLevel.Fine, ComputeReferences = true }), lst, ac);
            return lst;
        }

        private static void ProcGeo(GeometryElement g, List<Solid> lst, bool ac)
        {
            if (g == null) return;
            foreach (GeometryObject o in g)
            {
                if (o is Solid s && s.Volume > 0.0001 && s.Faces.Size > 0)
                { if (ac || s.Volume / s.SurfaceArea > 0.001) lst.Add(s); }
                else if (o is GeometryInstance gi) ProcGeo(gi.GetInstanceGeometry(), lst, ac);
            }
        }

        // ── Rutas de registro de auditoría ──────────────────
        // Estructura: [Proyecto]\Registro_BIM\YYYY-MM-DD_HHmm\OLD\  y  \NEW\
        internal static string RutaRegistro(Document doc)
        {
            string base_ = string.IsNullOrEmpty(doc.PathName)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PluginEstructural")
                : Path.GetDirectoryName(doc.PathName);
            return Path.Combine(base_, "Registro_BIM");
        }

        internal static string Safe(string s)
        {
            foreach (char c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return s;
        }

        internal static RevColor Verde    => new RevColor(0, 200, 80);
        internal static RevColor Amarillo => new RevColor(255, 200, 0);
        internal static RevColor Rojo     => new RevColor(220, 50, 50);

        // Exportar IFC a una carpeta concreta
        internal static void ExportarIFC(Document doc, string carpeta, string nombre)
        {
            Directory.CreateDirectory(carpeta);
            var opts = new IFCExportOptions
            {
                FileVersion          = IFCVersion.IFC2x3CV2,
                ExportBaseQuantities = true,
                WallAndColumnSplitting = false,
            };
            using (var t = new Transaction(doc, "Exportar IFC"))
            {
                t.Start();
                doc.Export(carpeta, nombre, opts);
                t.Commit();
            }
        }
    }

    // ═══════════════════════════════════════════════════════
    //  HASH LIGERO — para detectar cambios sin JSON externo
    // ═══════════════════════════════════════════════════════
    internal static class HashElem
    {
        internal static string Calcular(Element e)
        {
            var sb = new StringBuilder();
            sb.Append(e.Category?.Name ?? "");
            var bb = e.get_BoundingBox(null);
            if (bb != null) { sb.Append(bb.Min.X.ToString("F3")); sb.Append(bb.Min.Y.ToString("F3")); sb.Append(bb.Min.Z.ToString("F3")); sb.Append(bb.Max.X.ToString("F3")); sb.Append(bb.Max.Y.ToString("F3")); sb.Append(bb.Max.Z.ToString("F3")); }
            foreach (Parameter p in e.Parameters)
                try { if (p.HasValue && !p.IsReadOnly) sb.Append(p.AsValueString() ?? ""); } catch { }
            return sb.ToString().GetHashCode().ToString("X8");
        }
    }

    // ═══════════════════════════════════════════════════════
    //  MÓDULO 1 — EXPORTAR SAT
    // ═══════════════════════════════════════════════════════
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
                    ? uidoc.Selection.GetElementIds()
                    : IdsModelo(doc, dlg.Niveles);

                if (ids.Count == 0) { TaskDlg.Show("Atención", "Sin elementos en el alcance elegido."); return Result.Cancelled; }

                var sols = new List<Solid>();
                foreach (var id in ids)
                {
                    var e = doc.GetElement(id);
                    if (Utils.EsPermitido(e)) sols.AddRange(Utils.ObtenerSolidos(e));
                }
                if (sols.Count == 0) { TaskDlg.Show("Error", "Sin geometría válida."); return Result.Failed; }

                var gen = new List<ElementId>();
                using (var t = new Transaction(doc, "SAT — Sólido Único"))
                {
                    t.Start();
                    for (int i = 0; i < sols.Count; i += 100)
                        ProcesarLote(doc, sols.Skip(i).Take(100).ToList(), gen, i / 100 + 1);
                    if (gen.Count > 0) CrearVistaSAT(doc, gen);
                    t.Commit();
                }

                var vista = new FilteredElementCollector(doc).OfClass(typeof(View3D)).Cast<View3D>()
                    .FirstOrDefault(v => v.Name == "EXPORTACION_SAT");
                if (vista != null) uidoc.ActiveView = vista;

                TaskDlg.Show("✅ SAT Listo",
                    $"Extracción completa.\n• Sólidos: {sols.Count}\n• DirectShapes: {gen.Count}\n\n" +
                    "Vista 'EXPORTACION_SAT' activa.\nExporta: Archivo → Exportar → CAD → SAT");
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
                    var lp  = e.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM)
                           ?? e.get_Parameter(BuiltInParameter.SCHEDULE_LEVEL_PARAM);
                    if (lp != null)
                    {
                        var lvl = doc.GetElement(lp.AsElementId()) as Level;
                        if (lvl == null || !niveles.Contains(lvl.Name)) continue;
                    }
                }
                ids.Add(e.Id);
            }
            return ids;
        }

        void ProcesarLote(Document doc, List<Solid> chunk, List<ElementId> gen, int idx)
        {
            IList<GeometryObject> sh = chunk.Cast<GeometryObject>().ToList();
            try
            {
                var ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
                if (ds.IsValidShape(sh))
                {
                    ds.ApplicationId = "PluginEstructural"; ds.Name = $"SAT_{idx}"; ds.SetShape(sh);
                    ElementTransformUtils.RotateElement(doc, ds.Id, Line.CreateBound(XYZ.Zero, XYZ.BasisX), -Math.PI / 2.0);
                    gen.Add(ds.Id);
                }
                else { doc.Delete(ds.Id); foreach (var s in chunk) SolidoIndividual(doc, s, gen); }
            }
            catch { foreach (var s in chunk) SolidoIndividual(doc, s, gen); }
        }

        void SolidoIndividual(Document doc, Solid s, List<ElementId> gen)
        {
            try
            {
                IList<GeometryObject> sh = new List<GeometryObject> { s };
                var ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
                if (ds.IsValidShape(sh))
                {
                    ds.ApplicationId = "PluginEstructural"; ds.SetShape(sh);
                    ElementTransformUtils.RotateElement(doc, ds.Id, Line.CreateBound(XYZ.Zero, XYZ.BasisX), -Math.PI / 2.0);
                    gen.Add(ds.Id);
                }
                else doc.Delete(ds.Id);
            }
            catch { }
        }

        void CrearVistaSAT(Document doc, List<ElementId> gen)
        {
            var vft = new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>().FirstOrDefault(x => x.ViewFamily == ViewFamily.ThreeDimensional);
            if (vft == null) return;
            var old = new FilteredElementCollector(doc).OfClass(typeof(View3D)).Cast<View3D>()
                .FirstOrDefault(v => v.Name == "EXPORTACION_SAT");
            if (old != null) doc.Delete(old.Id);
            var v = View3D.CreateIsometric(doc, vft.Id);
            v.Name = "EXPORTACION_SAT"; v.DisplayStyle = DisplayStyle.Shading;
            v.IsolateElementsTemporary(gen); v.ConvertTemporaryHideIsolateToPermanent();
        }
    }

    // ═══════════════════════════════════════════════════════
    //  MÓDULO 2 — COMPARAR VERSIONES  (lógica OLD / NEW)
    //  Registro de auditoría en carpetas con fecha
    // ═══════════════════════════════════════════════════════
    internal enum AccionComp { GuardarOLD, CompararYGenerar, Limpiar }

    [Transaction(TransactionMode.Manual)]
    public class CmdCompararVersiones : IExternalCommand
    {
        public Result Execute(ExternalCommandData data, ref string msg, ElementSet es)
        {
            var doc      = data.Application.ActiveUIDocument.Document;
            string regDir = Utils.RutaRegistro(doc);
            string oldDir = BuscarUltimoOLD(regDir);

            using (var dlg = new VentanaComparar(oldDir != null, oldDir))
            {
                if (dlg.ShowDialog() != DialogResult.OK) return Result.Cancelled;

                switch (dlg.Op)
                {
                    case AccionComp.GuardarOLD:
                        return GuardarVersionOLD(doc, regDir);

                    case AccionComp.CompararYGenerar:
                        if (oldDir == null) { TaskDlg.Show("Sin versión anterior", "Primero guarda una versión OLD."); return Result.Cancelled; }
                        return CompararYColorear(doc, regDir, oldDir);

                    case AccionComp.Limpiar:
                        Limpiar(doc);
                        TaskDlg.Show("✅ Listo", "Colores de comparación eliminados de la vista activa.");
                        return Result.Succeeded;
                }
            }
            return Result.Succeeded;
        }

        // ── Busca la carpeta OLD más reciente ────────────────
        string BuscarUltimoOLD(string regDir)
        {
            if (!Directory.Exists(regDir)) return null;
            return Directory.GetDirectories(regDir, "*")
                .Where(d => Directory.Exists(Path.Combine(d, "OLD")))
                .OrderByDescending(d => d)
                .FirstOrDefault() is string carpeta
                    ? Path.Combine(carpeta, "OLD") : null;
        }

        // ── Exporta IFC como OLD con hash de elementos ───────
        Result GuardarVersionOLD(Document doc, string regDir)
        {
            string stamp   = DateTime.Now.ToString("yyyy-MM-dd_HHmm");
            string sesion  = Path.Combine(regDir, stamp);
            string oldPath = Path.Combine(sesion, "OLD");

            Utils.ExportarIFC(doc, oldPath, Utils.Safe(doc.Title) + "_OLD");

            // Guardar hashes de todos los elementos estructurales
            var hashes = new StringBuilder();
            hashes.AppendLine("UniqueId|CategoriaId|Hash");
            foreach (Element e in new FilteredElementCollector(doc).WhereElementIsNotElementType())
                if (Utils.EsPermitido(e))
                    hashes.AppendLine($"{e.UniqueId}|{e.Category?.Id.Value}|{HashElem.Calcular(e)}");

            File.WriteAllText(Path.Combine(oldPath, "hashes.csv"), hashes.ToString(), Encoding.UTF8);
            EscribirLog(sesion, "OLD guardado", doc, 0, 0, 0);

            TaskDlg.Show("✅ Versión OLD Guardada",
                $"Registro guardado en:\n{oldPath}\n\n" +
                $"  📄 IFC del estado actual\n  📋 hashes.csv con estado de {ContarEstructurales(doc)} elementos\n\n" +
                "Cuando hagas cambios en el modelo, usa\n'Comparar con versión anterior' para ver las diferencias.");
            return Result.Succeeded;
        }

        // ── Compara OLD vs modelo actual y colorea ───────────
        Result CompararYColorear(Document doc, string regDir, string oldHashDir)
        {
            // Leer hashes del OLD
            string hashFile = Path.Combine(oldHashDir, "hashes.csv");
            if (!File.Exists(hashFile)) { TaskDlg.Show("Error", $"No se encontró hashes.csv en:\n{oldHashDir}"); return Result.Failed; }

            var anterior = new Dictionary<string, string>(); // UniqueId → Hash
            foreach (var linea in File.ReadAllLines(hashFile).Skip(1))
            {
                var p = linea.Split('|');
                if (p.Length >= 3) anterior[p[0]] = p[2];
            }

            // Estado actual
            var actuales = new FilteredElementCollector(doc).WhereElementIsNotElementType()
                .Where(Utils.EsPermitido).ToDictionary(e => e.UniqueId);

            var nuevos      = actuales.Where(kv => !anterior.ContainsKey(kv.Key)).Select(kv => kv.Value).ToList();
            var modificados = actuales.Where(kv => anterior.ContainsKey(kv.Key) && HashElem.Calcular(kv.Value) != anterior[kv.Key]).Select(kv => kv.Value).ToList();
            int eliminados  = anterior.Keys.Count(uid => !actuales.ContainsKey(uid));

            // Colorear vista activa
            RevitView vista  = doc.ActiveView;
            var  patron = new FilteredElementCollector(doc).OfClass(typeof(FillPatternElement))
                .Cast<FillPatternElement>().FirstOrDefault(fp => fp.GetFillPattern().IsSolidFill);

            using (var t = new Transaction(doc, "Colorear Cambios BIM"))
            {
                t.Start();
                foreach (var e in actuales.Values)
                    try { vista.SetElementOverrides(e.Id, new OverrideGraphicSettings()); } catch { }
                AplicarColor(vista, nuevos,      Utils.Verde,    patron);
                AplicarColor(vista, modificados, Utils.Amarillo, patron);
                t.Commit();
            }

            // Exportar IFC NEW en nueva sesión con el mismo timestamp
            string stampSesion = Path.GetFileName(Path.GetDirectoryName(oldHashDir));
            string sesionDir   = Path.Combine(regDir, stampSesion);
            string newPath     = Path.Combine(sesionDir, "NEW");
            Utils.ExportarIFC(doc, newPath, Utils.Safe(doc.Title) + "_NEW");

            // Log de auditoría
            EscribirLog(sesionDir, "Comparación completada", doc, nuevos.Count, modificados.Count, eliminados);

            // Resumen de diferencias en CSV para auditoría
            EscribirDiferenciasCSV(sesionDir, doc, nuevos, modificados, anterior.Keys.Where(uid => !actuales.ContainsKey(uid)).ToList());

            TaskDlg.Show("📊 Comparación Completada",
                $"Vista activa: '{vista.Name}'\n\n" +
                $"🟢 Nuevos:      {nuevos.Count}\n" +
                $"🟡 Modificados: {modificados.Count}\n" +
                $"🔴 Eliminados:  {eliminados}\n\n" +
                $"Registro de auditoría guardado en:\n{sesionDir}\n\n" +
                "Usa 'Generar Viewer' para compartir con obra.");
            return Result.Succeeded;
        }

        void AplicarColor(RevitView v, List<Element> elems, RevColor col, FillPatternElement pat)
        {
            var ogs = new OverrideGraphicSettings();
            ogs.SetProjectionLineColor(col);
            ogs.SetSurfaceForegroundPatternColor(col);
            ogs.SetSurfaceForegroundPatternVisible(true);
            if (pat != null) ogs.SetSurfaceForegroundPatternId(pat.Id);
            ogs.SetSurfaceTransparency(30);
            foreach (var e in elems) try { v.SetElementOverrides(e.Id, ogs); } catch { }
        }

        void Limpiar(Document doc)
        {
            using (var t = new Transaction(doc, "Limpiar Colores"))
            {
                t.Start();
                var v = doc.ActiveView;
                foreach (Element e in new FilteredElementCollector(doc).WhereElementIsNotElementType())
                    if (Utils.EsPermitido(e)) try { v.SetElementOverrides(e.Id, new OverrideGraphicSettings()); } catch { }
                t.Commit();
            }
        }

        int ContarEstructurales(Document doc) =>
            new FilteredElementCollector(doc).WhereElementIsNotElementType().Count(Utils.EsPermitido);

        void EscribirLog(string sesion, string accion, Document doc, int nuevos, int mods, int elim)
        {
            string logFile = Path.Combine(sesion, "auditoria.log");
            var sb = new StringBuilder();
            if (File.Exists(logFile)) sb.Append(File.ReadAllText(logFile));
            sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]");
            sb.AppendLine($"  Acción:    {accion}");
            sb.AppendLine($"  Proyecto:  {doc.Title}");
            sb.AppendLine($"  Usuario:   {Environment.UserName}");
            if (nuevos > 0 || mods > 0 || elim > 0)
            {
                sb.AppendLine($"  Nuevos:    {nuevos}");
                sb.AppendLine($"  Modificados: {mods}");
                sb.AppendLine($"  Eliminados:  {elim}");
            }
            sb.AppendLine();
            File.WriteAllText(logFile, sb.ToString(), Encoding.UTF8);
        }

        void EscribirDiferenciasCSV(string sesion, Document doc, List<Element> nuevos, List<Element> mods, IEnumerable<string> elimIds)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Estado|UniqueId|Categoria|Tipo");
            foreach (var e in nuevos)      sb.AppendLine($"NUEVO|{e.UniqueId}|{e.Category?.Name}|{e.Name}");
            foreach (var e in mods)        sb.AppendLine($"MODIFICADO|{e.UniqueId}|{e.Category?.Name}|{e.Name}");
            foreach (var uid in elimIds)   sb.AppendLine($"ELIMINADO|{uid}||");
            File.WriteAllText(Path.Combine(sesion, "diferencias.csv"), sb.ToString(), Encoding.UTF8);
        }
    }

    // ═══════════════════════════════════════════════════════
    //  MÓDULO 3 — GENERAR VIEWER HTML
    // ═══════════════════════════════════════════════════════
    [Transaction(TransactionMode.Manual)]
    public class CmdGenerarViewer : IExternalCommand
    {
        public Result Execute(ExternalCommandData data, ref string msg, ElementSet es)
        {
            var doc = data.Application.ActiveUIDocument.Document;

            using (var dlg = new VentanaViewer(doc))
            {
                if (dlg.ShowDialog() != DialogResult.OK) return Result.Cancelled;

                string carpeta = dlg.CarpetaSalida;
                if (!Directory.Exists(carpeta)) { TaskDlg.Show("Error", "Carpeta inválida o no existe."); return Result.Cancelled; }

                string nomIFC = Utils.Safe(doc.Title) + "_viewer";
                Utils.ExportarIFC(doc, carpeta, nomIFC);
                File.WriteAllText(Path.Combine(carpeta, "viewer.html"),       GenerarHTML(nomIFC + ".ifc", doc.Title), Encoding.UTF8);
                File.WriteAllText(Path.Combine(carpeta, "INSTRUCCIONES.txt"), Instrucciones(doc.Title));

                TaskDlg.Show("✅ Viewer Generado",
                    $"Archivos en:\n{carpeta}\n\n" +
                    $"📄 {nomIFC}.ifc\n🌐 viewer.html\n📋 INSTRUCCIONES.txt\n\n" +
                    "Envía la carpeta completa a obra por correo, SharePoint o Drive.");

                System.Diagnostics.Process.Start("explorer.exe", carpeta);
            }
            return Result.Succeeded;
        }

        string GenerarHTML(string nomIFC, string titulo) => $@"<!DOCTYPE html>
<html lang=""es""><head>
<meta charset=""UTF-8""/><meta name=""viewport"" content=""width=device-width,initial-scale=1""/>
<title>Viewer BIM — {titulo}</title>
<style>
*{{margin:0;padding:0;box-sizing:border-box}}
body{{font-family:'Segoe UI',sans-serif;background:#1a1a2e;color:#eee;overflow:hidden}}
#hdr{{position:fixed;top:0;left:0;right:0;height:50px;background:linear-gradient(90deg,#0f3460,#16213e);
  display:flex;align-items:center;padding:0 18px;z-index:100;box-shadow:0 2px 8px #0006}}
#hdr h1{{font-size:15px;color:#e0e0e0}}#hdr span{{font-size:11px;color:#888;margin-left:10px}}
#tb{{position:fixed;top:50px;left:0;right:0;height:42px;background:#16213e;
  display:flex;align-items:center;padding:0 12px;gap:7px;z-index:99;border-bottom:1px solid #0f3460}}
.btn{{background:#0f3460;color:#ccc;border:1px solid #1a4a7a;padding:5px 12px;
  border-radius:4px;cursor:pointer;font-size:12px;transition:.2s}}
.btn:hover{{background:#1a4a7a;color:#fff}}.btn.on{{background:#e94560;border-color:#e94560;color:#fff}}
#leg{{position:fixed;bottom:16px;left:16px;background:rgba(15,52,96,.93);padding:10px 14px;
  border-radius:8px;z-index:99;border:1px solid #1a4a7a;min-width:165px}}
#leg h3{{font-size:10px;color:#aaa;margin-bottom:6px;text-transform:uppercase;letter-spacing:1px}}
.li{{display:flex;align-items:center;gap:7px;margin:4px 0;font-size:12px}}
.dot{{width:12px;height:12px;border-radius:3px;flex-shrink:0}}
#inf{{position:fixed;right:0;top:92px;width:265px;bottom:0;background:#16213e;
  border-left:1px solid #0f3460;padding:14px;overflow-y:auto;z-index:99;display:none}}
#inf h3{{font-size:11px;color:#888;margin-bottom:9px;text-transform:uppercase}}
.pr{{display:flex;gap:6px;margin:3px 0;font-size:11px}}
.pk{{color:#888;min-width:90px}}.pv{{color:#ccc;word-break:break-all}}
#vc{{position:fixed;top:92px;left:0;right:0;bottom:0}}
#st{{position:fixed;bottom:16px;right:16px;background:rgba(15,52,96,.9);
  padding:6px 13px;border-radius:4px;font-size:11px;color:#aaa;z-index:99}}
#ov{{position:fixed;inset:0;background:#1a1a2e;display:flex;flex-direction:column;
  align-items:center;justify-content:center;z-index:1000}}
.sp{{width:44px;height:44px;border:4px solid #0f3460;border-top-color:#e94560;
  border-radius:50%;animation:spin 1s linear infinite;margin-bottom:16px}}
@keyframes spin{{to{{transform:rotate(360deg)}}}}
#pg{{font-size:13px;color:#888;margin-top:6px}}
</style></head><body>
<div id=""ov"">
  <div class=""sp""></div>
  <div style=""font-size:17px;color:#ccc"">Cargando modelo BIM...</div>
  <div id=""pg"">Iniciando viewer...</div>
</div>
<div id=""hdr""><h1>📐 Viewer BIM Estructural</h1><span>— {titulo}</span></div>
<div id=""tb"">
  <button class=""btn"" id=""bCar"">📂 Cargar IFC</button>
  <button class=""btn"" id=""bMed"">📏 Medir</button>
  <button class=""btn"" id=""bSec"">✂️ Sección</button>
  <button class=""btn"" id=""bRes"">🔄 Reset</button>
  <button class=""btn"" id=""bInf"">ℹ️ Props</button>
  <span style=""margin-left:auto;font-size:10px;color:#555"">Clic=propiedades | ESC=limpiar</span>
</div>
<div id=""vc""><canvas id=""cv"" style=""width:100%;height:100%""></canvas></div>
<div id=""leg"">
  <h3>Leyenda de Cambios</h3>
  <div class=""li""><div class=""dot"" style=""background:#00C850""></div>Nuevos</div>
  <div class=""li""><div class=""dot"" style=""background:#FFC800""></div>Modificados</div>
  <div class=""li""><div class=""dot"" style=""background:#DC3232""></div>Eliminados</div>
  <div class=""li""><div class=""dot"" style=""background:#607080""></div>Sin cambios</div>
</div>
<div id=""inf""><h3>Propiedades del Elemento</h3>
  <div id=""pc""><p style=""color:#555;font-size:11px"">Haz clic en un elemento.</p></div>
</div>
<div id=""st"">Listo — {nomIFC}</div>
<script type=""module"">
import {{IfcViewerAPI}} from 'https://unpkg.com/web-ifc-viewer@1.0.221/dist/web-ifc-viewer.js';
const vc=document.getElementById('cv').parentElement,ov=document.getElementById('ov');
const pg=document.getElementById('pg'),st=document.getElementById('st');
const inf=document.getElementById('inf'),pc=document.getElementById('pc');
const viewer=new IfcViewerAPI({{container:vc,backgroundColor:new THREE.Color(0x1a1a2e)}});
viewer.grid.setGrid();viewer.axes.setAxes();
let mMed=false,mSec=false;
async function auto(){{
  try{{
    pg.textContent='Cargando {nomIFC}...';
    const m=await viewer.IFC.loadIfcUrl('./{nomIFC}',true,p=>pg.textContent=Math.round(p*100)+'%');
    ov.style.display='none';st.textContent='Modelo cargado ✓';
    try{{viewer.shadowDropper.renderShadow(m.modelID);}}catch{{}}
  }}catch(e){{pg.textContent='Usa 📂 Cargar IFC para abrir manualmente';setTimeout(()=>ov.style.display='none',3000)}}
}}
auto();
document.getElementById('bCar').onclick=()=>{{
  const inp=document.createElement('input');inp.type='file';inp.accept='.ifc';
  inp.onchange=async e=>{{
    const f=e.target.files[0];if(!f)return;
    ov.style.display='flex';pg.textContent='Cargando '+f.name+'...';
    try{{
      const m=await viewer.IFC.loadIfcUrl(URL.createObjectURL(f),true,p=>pg.textContent=Math.round(p*100)+'%');
      ov.style.display='none';st.textContent=f.name+' ✓';
      try{{viewer.shadowDropper.renderShadow(m.modelID);}}catch{{}}
    }}catch(err){{ov.style.display='none';st.textContent='Error: '+err.message}}
  }};inp.click();
}};
document.getElementById('bMed').onclick=()=>{{
  mMed=!mMed;document.getElementById('bMed').classList.toggle('on',mMed);
  viewer.dimensions.active=mMed;viewer.dimensions.previewActive=mMed;
  st.textContent=mMed?'📏 Clic en 2 puntos para medir | ESC para borrar':'Medición desactivada';
}};
document.getElementById('bSec').onclick=()=>{{
  mSec=!mSec;document.getElementById('bSec').classList.toggle('on',mSec);
  viewer.clipper.active=mSec;
  if(!mSec)viewer.clipper.deleteAllPlanes();
  st.textContent=mSec?'✂️ Doble clic en el modelo para crear plano de corte':'Sección desactivada';
}};
document.getElementById('bRes').onclick=()=>{{
  viewer.context.resetCamera();viewer.IFC.unpickIfcItems();st.textContent='Vista reiniciada';
}};
document.getElementById('bInf').onclick=()=>{{
  const ab=inf.style.display==='block';
  inf.style.display=ab?'none':'block';
  document.getElementById('vc').style.right=ab?'0':'265px';
}};
window.addEventListener('click',async()=>{{
  if(mMed){{viewer.dimensions.create();return}}
  if(mSec){{viewer.clipper.createPlane();return}}
  const r=await viewer.IFC.selector.pickIfcItem(true);
  if(!r)return;
  try{{
    const props=await viewer.IFC.getProperties(r.modelID,r.id,true,false);
    let h='';
    for(const[k,v]of Object.entries(props)){{
      const val=typeof v==='object'&&v!==null?v.value??'—':v;
      if(typeof val!=='object')h+=`<div class=""pr""><span class=""pk"">${{k}}</span><span class=""pv"">${{val}}</span></div>`;
    }}
    pc.innerHTML=h||'<p style=""color:#555"">Sin propiedades</p>';
    inf.style.display='block';document.getElementById('vc').style.right='265px';
    st.textContent='Seleccionado ID: '+r.id;
  }}catch{{st.textContent='Sin propiedades disponibles para este elemento'}}
}});
window.addEventListener('dblclick',()=>{{if(mMed)viewer.dimensions.create();}});
window.addEventListener('keydown',e=>{{
  if(e.key==='Escape'){{viewer.IFC.selector.unpickIfcItems();viewer.dimensions.deleteAll();}}
}});
</script></body></html>";

        string Instrucciones(string t) =>
$@"VIEWER BIM ESTRUCTURAL — {t}
Generado: {DateTime.Now:dd/MM/yyyy HH:mm}
Usuario:  {Environment.UserName}

ABRIR:
  1. NO separar los archivos de esta carpeta
  2. Doble clic en viewer.html (Chrome o Edge)
  3. Si no carga automatico: boton [📂 Cargar IFC]

HERRAMIENTAS:
  📏 Medir   — Activar boton, clic punto 1, clic punto 2, ESC para borrar
  ✂️ Seccion — Activar boton, doble clic en el modelo
  ℹ️ Props   — Activar boton, clic en cualquier elemento
  🔄 Reset   — Restaura la vista original

LEYENDA DE COLORES:
  Verde    = Elementos NUEVOS en esta version
  Amarillo = Elementos MODIFICADOS
  Rojo     = Elementos ELIMINADOS (referencia)
  Gris     = Sin cambios

REQUISITO: Chrome o Edge actualizado. Sin instalacion adicional.";
    }

    // ═══════════════════════════════════════════════════════
    //  UI — VENTANA SAT
    // ═══════════════════════════════════════════════════════
    internal class VentanaSAT : System.Windows.Forms.Form
    {
        public bool         UsarSeleccion { get; private set; }
        public List<string> Niveles       { get; private set; } = new List<string>();

        RadioButton rbSel, rbTodo, rbNiv;
        CheckedListBox clb;

        public VentanaSAT(Document doc)
        {
            Text = "Exportar SAT — BIM Estructural";
            Size = new WinSize(430, 415); StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false;
            BackColor = WinColor.FromArgb(26, 26, 46); ForeColor = WinColor.FromArgb(220, 220, 220);

            Controls.Add(new WinLabel { Text = "📐  EXPORTAR SÓLIDO SAT — SolidWorks", Location = new WinPoint(15, 14), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = WinColor.FromArgb(100, 180, 255) });
            Controls.Add(new WinLabel { Text = "Alcance de exportación:", Location = new WinPoint(15, 44), AutoSize = true, Font = new Font("Segoe UI", 9), ForeColor = WinColor.FromArgb(155, 155, 155) });

            rbSel  = new RadioButton { Text = "Usar selección activa en Revit",  Location = new WinPoint(20, 68),  AutoSize = true, Checked = true, ForeColor = WinColor.FromArgb(200, 200, 200), Font = new Font("Segoe UI", 9) };
            rbTodo = new RadioButton { Text = "Todo el modelo estructural",       Location = new WinPoint(20, 92),  AutoSize = true, ForeColor = WinColor.FromArgb(200, 200, 200), Font = new Font("Segoe UI", 9) };
            rbNiv  = new RadioButton { Text = "Solo niveles específicos:",        Location = new WinPoint(20, 116), AutoSize = true, ForeColor = WinColor.FromArgb(200, 200, 200), Font = new Font("Segoe UI", 9) };
            rbNiv.CheckedChanged += (s, e) => clb.Enabled = rbNiv.Checked;

            clb = new CheckedListBox
            {
                Location = new WinPoint(38, 142), Size = new WinSize(360, 145), Enabled = false,
                BackColor = WinColor.FromArgb(22, 33, 62), ForeColor = WinColor.FromArgb(200, 200, 200),
                Font = new Font("Segoe UI", 9), BorderStyle = BorderStyle.FixedSingle
            };
            foreach (Level lv in new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().OrderBy(l => l.Elevation))
                clb.Items.Add(lv.Name);

            var pI = new WinPanel { Location = new WinPoint(15, 298), Size = new WinSize(390, 50), BackColor = WinColor.FromArgb(15, 52, 96) };
            pI.Controls.Add(new WinLabel
            {
                Text = "ℹ️  Sólidos en vista 'EXPORTACION_SAT', rotados -90° en X para SolidWorks.\n   Exporta: Archivo → Exportar → CAD → SAT",
                Location = new WinPoint(8, 8), AutoSize = true, Font = new Font("Segoe UI", 8), ForeColor = WinColor.FromArgb(140, 180, 220)
            });
            Controls.Add(pI);

            var bOK  = new System.Windows.Forms.Button { Text = "✅  Exportar", Location = new WinPoint(228, 360), Size = new WinSize(92, 28), BackColor = WinColor.FromArgb(0, 120, 60),  ForeColor = WinColor.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8, FontStyle.Bold) };
            var bCan = new System.Windows.Forms.Button { Text = "Cancelar",    Location = new WinPoint(328, 360), Size = new WinSize(82, 28), BackColor = WinColor.FromArgb(70, 30, 30), ForeColor = WinColor.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8), DialogResult = DialogResult.Cancel };
            bOK.Click += (s, e) =>
            {
                UsarSeleccion = rbSel.Checked;
                if (rbNiv.Checked) Niveles = clb.CheckedItems.Cast<string>().ToList();
                DialogResult = DialogResult.OK; Close();
            };

            Controls.AddRange(new System.Windows.Forms.Control[] { rbSel, rbTodo, rbNiv, clb, bOK, bCan });
            AcceptButton = bOK; CancelButton = bCan;
        }
    }

    // ═══════════════════════════════════════════════════════
    //  UI — VENTANA COMPARAR (con lógica OLD/NEW explicada)
    // ═══════════════════════════════════════════════════════
    internal class VentanaComparar : System.Windows.Forms.Form
    {
        public AccionComp Op { get; private set; }

        public VentanaComparar(bool hayOLD, string rutaOLD)
        {
            Text = "Comparar Versiones — BIM Estructural";
            Size = new WinSize(460, 370); StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false;
            BackColor = WinColor.FromArgb(26, 26, 46); ForeColor = WinColor.FromArgb(220, 220, 220);

            Controls.Add(new WinLabel { Text = "📊  COMPARADOR DE VERSIONES — REGISTRO AUDITORÍA", Location = new WinPoint(15, 14), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = WinColor.FromArgb(100, 180, 255) });

            // Estado del OLD
            string estadoTxt = hayOLD
                ? $"✅  Versión OLD encontrada:\n   {rutaOLD}"
                : "⚠️  Sin versión OLD — guarda una primero";
            Controls.Add(new WinLabel
            {
                Text = estadoTxt, Location = new WinPoint(15, 46), AutoSize = true,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = hayOLD ? WinColor.FromArgb(0, 200, 100) : WinColor.FromArgb(255, 180, 0)
            });

            // Explicación del flujo
            var pFlujo = new WinPanel { Location = new WinPoint(15, 88), Size = new WinSize(425, 38), BackColor = WinColor.FromArgb(10, 25, 50) };
            pFlujo.Controls.Add(new WinLabel
            {
                Text = "Flujo:  [1° semana → Guardar OLD]   →   [Hacer cambios en Revit]   →   [Comparar y generar NEW]",
                Location = new WinPoint(8, 10), AutoSize = true, Font = new Font("Segoe UI", 8), ForeColor = WinColor.FromArgb(130, 170, 210)
            });
            Controls.Add(pFlujo);

            // Botones de acción
            BtnGrande("💾  Guardar versión OLD (línea base)\n    Exporta IFC + hashes del estado actual para referencia futura",
                136, WinColor.FromArgb(15, 52, 96), true,
                () => { Op = AccionComp.GuardarOLD; DialogResult = DialogResult.OK; Close(); });

            BtnGrande("🔍  Comparar con OLD y generar NEW\n    Colorea cambios en vista + exporta IFC NEW + genera CSV de auditoría",
                204, hayOLD ? WinColor.FromArgb(0, 70, 35) : WinColor.FromArgb(35, 35, 35), hayOLD,
                () => { Op = AccionComp.CompararYGenerar; DialogResult = DialogResult.OK; Close(); });

            BtnGrande("🧹  Limpiar colores de la vista activa",
                272, WinColor.FromArgb(55, 22, 22), true,
                () => { Op = AccionComp.Limpiar; DialogResult = DialogResult.OK; Close(); });

            var bCan = new System.Windows.Forms.Button { Text = "Cancelar", Location = new WinPoint(360, 315), Size = new WinSize(80, 26), BackColor = WinColor.FromArgb(50, 50, 60), ForeColor = WinColor.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8), DialogResult = DialogResult.Cancel };
            Controls.Add(bCan); CancelButton = bCan;
        }

        void BtnGrande(string txt, int y, WinColor bg, bool en, Action fn)
        {
            var b = new System.Windows.Forms.Button
            {
                Text = txt, Location = new WinPoint(15, y), Size = new WinSize(425, 54),
                BackColor = bg, ForeColor = en ? WinColor.White : WinColor.FromArgb(65, 65, 65),
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9),
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(12, 0, 0, 0), Enabled = en
            };
            b.Click += (s, e) => fn();
            Controls.Add(b);
        }
    }

    // ═══════════════════════════════════════════════════════
    //  UI — VENTANA VIEWER
    // ═══════════════════════════════════════════════════════
    internal class VentanaViewer : System.Windows.Forms.Form
    {
        public string CarpetaSalida { get; private set; }
        System.Windows.Forms.TextBox tx;

        public VentanaViewer(Document doc)
        {
            Text = "Generar Viewer — BIM Estructural";
            Size = new WinSize(450, 355); StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false;
            BackColor = WinColor.FromArgb(26, 26, 46); ForeColor = WinColor.FromArgb(220, 220, 220);

            Controls.Add(new WinLabel { Text = "🌐  GENERAR VIEWER PARA OBRA", Location = new WinPoint(15, 14), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = WinColor.FromArgb(100, 180, 255) });

            var pI = new WinPanel { Location = new WinPoint(15, 46), Size = new WinSize(412, 82), BackColor = WinColor.FromArgb(15, 52, 96) };
            pI.Controls.Add(new WinLabel
            {
                Text = "Genera en la carpeta seleccionada:\n\n  📄  IFC con colores de cambios aplicados\n  🌐  viewer.html — Chrome/Edge, sin instalación\n  📋  INSTRUCCIONES.txt para el equipo en obra",
                Location = new WinPoint(10, 8), AutoSize = true, Font = new Font("Segoe UI", 8.5f), ForeColor = WinColor.FromArgb(160, 200, 240)
            });
            Controls.Add(pI);

            Controls.Add(new WinLabel { Text = "Carpeta de salida:", Location = new WinPoint(15, 144), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) });

            string def = string.IsNullOrEmpty(doc.PathName)
                ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                : Path.GetDirectoryName(doc.PathName);

            tx = new System.Windows.Forms.TextBox
            {
                Text = def, Location = new WinPoint(15, 165), Size = new WinSize(318, 24),
                BackColor = WinColor.FromArgb(22, 33, 62), ForeColor = WinColor.FromArgb(200, 200, 200),
                Font = new Font("Segoe UI", 8.5f), BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(tx);

            var bEx = new System.Windows.Forms.Button { Text = "📁 Examinar", Location = new WinPoint(340, 163), Size = new WinSize(88, 28), BackColor = WinColor.FromArgb(15, 52, 96), ForeColor = WinColor.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8) };
            bEx.Click += (s, e) =>
            {
                using (var fbd = new FolderBrowserDialog { SelectedPath = tx.Text, Description = "Selecciona la carpeta donde se guardará el viewer" })
                    if (fbd.ShowDialog() == DialogResult.OK) tx.Text = fbd.SelectedPath;
            };
            Controls.Add(bEx);

            var pAdv = new WinPanel { Location = new WinPoint(15, 202), Size = new WinSize(412, 50), BackColor = WinColor.FromArgb(50, 38, 8) };
            pAdv.Controls.Add(new WinLabel { Text = "⚠️  Aplica primero 'Comparar Versiones' para colorear los cambios.\n    El viewer exporta exactamente lo visible en la vista activa.", Location = new WinPoint(8, 7), AutoSize = true, Font = new Font("Segoe UI", 8), ForeColor = WinColor.FromArgb(255, 200, 100) });
            Controls.Add(pAdv);

            var bOK  = new System.Windows.Forms.Button { Text = "🚀  Generar Viewer", Location = new WinPoint(240, 278), Size = new WinSize(118, 30), BackColor = WinColor.FromArgb(0, 120, 60), ForeColor = WinColor.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            var bCan = new System.Windows.Forms.Button { Text = "Cancelar",          Location = new WinPoint(366, 278), Size = new WinSize(68,  30), BackColor = WinColor.FromArgb(50, 50, 60), ForeColor = WinColor.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8), DialogResult = DialogResult.Cancel };
            bOK.Click += (s, e) => { CarpetaSalida = tx.Text; DialogResult = DialogResult.OK; Close(); };
            Controls.AddRange(new System.Windows.Forms.Control[] { bOK, bCan });
            AcceptButton = bOK; CancelButton = bCan;
        }
    }
}