# Plugin BIM Estructural — Postensa Design & Build
## Rol del Agente
Eres el desarrollador principal de este plugin de Revit 2025.
Tienes permiso total para leer, modificar y crear archivos en esta carpeta.
Compila con `dotnet build` para verificar cada cambio.
Nunca termines una tarea sin compilar y confirmar 0 errores.

## Estructura del Proyecto
```
PluginEstructural/
├── PluginEstructural.cs       ← Código principal
├── PluginEstructural.csproj   ← Referencias y paquetes NuGet
├── PluginEstructural.addin    ← Registro del plugin en Revit
├── config.env                 ← Credenciales (NUNCA a git)
├── .gitignore
├── AGENT.md                   ← Este archivo
├── README.md
├── ThisApplication.cs
└── ThisApplication.Designer.cs
```

## Credenciales en config.env
- GEMINI_API_KEY      — Gemini 1.5 Flash para descripciones de cambios
- APS_CLIENT_ID       — Autodesk Platform Services (reservado para futuro)
- APS_CLIENT_SECRET   — Autodesk Platform Services (reservado para futuro)
- SPECKLE_TOKEN       — Speckle (reservado para futuro)
Leer SIEMPRE con Config.Get("KEY") — nunca hardcodear en código.

## Arquitectura del Plugin

### Clases principales
- `Config`              — Lee config.env de forma segura con caché
- `Esti`                — Paleta de colores y controles UI azul Postensa
- `Utils`               — Categorías Revit, sólidos, IFC, ocultar ruido
- `HashElem`            — Hash de elementos + parámetros legibles
- `Gemini`              — API Gemini para descripciones en español
- `AppInicio`           — Ribbon con 4 botones en tab "BIM Estructural"

### Comandos
- `CmdExportarSAT`         — Sólido unificado -90° para SolidWorks
- `CmdCompararVersiones`   — OLD/NEW + colores + reporte Excel + Gemini
- `CmdGenerarViewer`       — Viewer HTML local con IFC.js (sin APIs)
- `CmdCompartirVista`      — Abre Vistas Compartidas nativas de Revit

### Ventanas
- `VentanaSAT`      — UI azul con header logo Postensa
- `VentanaComparar` — UI azul con header logo Postensa
- `VentanaViewer`   — UI azul con header logo Postensa

## Estado Actual de Funcionalidades
| Módulo | Estado | Notas |
|--------|--------|-------|
| Exportar SAT | ✅ OK | Rotación -90°, vista EXPORTACION_SAT |
| Comparar OLD/NEW | ✅ OK | Colores verde/amarillo en vista |
| Vista COMPARACION_BIM | ✅ OK | Categorías importadas ocultas |
| Elementos sin cambios | ✅ OK | 75% transparencia |
| Gemini descripciones | ✅ OK | Fallback automático si falla |
| Reporte Excel (.xlsx) | ⚠️ PENDIENTE | Actualmente CSV, migrar a ClosedXML |
| Viewer HTML local | ⚠️ PENDIENTE | Reemplazar Speckle/APS con IFC.js local |
| Vista Compartida Revit | ⚠️ PENDIENTE | Botón 4to que abre SharedViews nativo |
| Logo Postensa en UI | ⚠️ PENDIENTE | Descargar PNG de postensa.com.mx |
| Iconos ribbon | ⚠️ PENDIENTE | BitmapImage de color por botón |

## Tareas Pendientes (en orden de prioridad)

### 1. VIEWER HTML LOCAL — reemplazar Speckle/APS
- Eliminar clases `Speckle` y `APS` del código
- Eliminar usings relacionados
- Crear carpeta VIEWER_OBRA en la carpeta de salida
- Copiar IFC como modelo.ifc dentro de VIEWER_OBRA
- Generar index.html con viewer IFC.js usando:
  * three.js desde unpkg.com
  * web-ifc-three@0.0.126 desde unpkg.com
  * OrbitControls para navegación
  * Fondo oscuro #0D1B2A estilo Postensa
  * Leyenda de colores fija en esquina
  * Header con logo POSTENSA y nombre del proyecto
- Abrir VIEWER_OBRA en Explorer al terminar
- El HTML debe funcionar abriendo directamente en Chrome sin servidor

### 2. EXCEL CON CLOSEDXML
- Agregar al .csproj:
  `<PackageReference Include="ClosedXML" Version="0.102.1" />`
- Reemplazar GenerarReporte para generar .xlsx con:
  * Hoja "REPORTE BIM": título combinado, encabezados azul #1CA0E6,
    datos con fondo alternado, columna Estado con color por tipo,
    anchos: No.=6 Estado=14 Descripcion=50 Categoria=22 Tipo=28 ID=38
  * Hoja "AUDITORIA": datos técnicos con bordes
  * Colores: NUEVO=#27C96F MODIFICADO=#FFC400 ELIMINADO=#E53935
  * Fila resumen al final con totales
  * Nombre: REPORTE_BIM_{yyyyMMdd_HHmm}.xlsx
  * Abrir automáticamente con Process.Start

### 3. LOGO POSTENSA EN VENTANAS
- Descargar PNG desde:
  https://postensa.com.mx/wp-content/uploads/2025/05/cropped-PostensaGlow-1-1-1-120x79.png
- Mostrar en PictureBox dentro del HeaderLogo de las 3 ventanas
- Fallback a texto "POSTENSA" si la descarga falla

### 4. ICONOS DE COLOR EN RIBBON
- Generar BitmapImage de 32x32 por código para cada botón:
  * SAT        → azul   (22, 160, 230)
  * Comparar   → verde  (39, 201, 111)
  * Viewer     → naranja (255, 152, 0)
  * Compartir  → morado (156, 39, 176)
- Asignar LargeImage e Image a cada PushButtonData
- Agregar referencias al .csproj si faltan:
  `<Reference Include="PresentationCore" />`
  `<Reference Include="WindowsBase" />`

### 5. BOTÓN VISTA COMPARTIDA
- 4to botón en ribbon: "Vista\nCompartida"
- Clase CmdCompartirVista que ejecuta PostableCommand.SharedViews
- Fallback: abrir viewer.autodesk.com en browser

## Reglas Importantes
1. NUNCA tocar: lógica OLD/NEW, HashElem, Utils.EsPermitido, CmdExportarSAT
2. SIEMPRE leer credenciales con Config.Get("KEY")
3. SIEMPRE compilar al final: dotnet build → 0 errores
4. El DLL compilado va automáticamente a Addins\2025\ según el .csproj
5. Si una API externa falla, siempre tener fallback local
6. Las ventanas SIEMPRE tienen HeaderLogo(468) como primer control
7. La vista COMPARACION_BIM debe poder recrearse sin error (cambiar vista activa antes de borrar)

## Compilar y Probar
```bash
# Compilar
dotnet build

# Verificar DLL generado
ls "C:\ProgramData\Autodesk\Revit\Addins\2025\PluginEstructural.dll"

# Flujo de prueba en Revit:
# 1. Cerrar Revit
# 2. dotnet build
# 3. Abrir Revit 2025
# 4. Tab "BIM Estructural" → probar cada botón
```

## Notas Técnicas
- Revit 2025 usa .NET Framework 4.8
- El DLL no se puede reemplazar con Revit abierto (bloqueado)
- Config cachea los valores — reiniciar Revit para releer config.env
- Las transacciones de Revit deben cerrarse antes de cambiar la vista activa
- ClosedXML requiere dotnet restore después de agregar al .csproj
- El viewer HTML debe usar type="module" para imports de ES6
- web-ifc necesita los archivos .wasm accesibles — usar unpkg.com CDN