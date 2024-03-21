v1.8.0
 - fix: #35 
 - fix: #36 
 - fix: ParameterTyp NumberInt calculation
 - fix: UId of imported Helptexts from OpenknxModules
 - fix: import knxprod empty DPT of ComObjectRef
 - fix: allocator calculation
 - added: checkbox for parametertype to export enums in header

v1.7.4
 - added: hyperlink from
   - Parameter to ParameterType
   - ParameterRef to Parameter
   - ComObjectRef to ComObject
 - added: checks for ParameterType Time
 - added: define for ParameterType Enums
 - fixed: DynamicView show only Modules/Repeater if activated

v1.7.3
 - fixed: Heatmap after split to Kaenx-Creator-Share [#30](https://github.com/OpenKNX/Kaenx-Creator/pull/30)
 - added: more info for importing app [29](https://github.com/OpenKNX/Kaenx-Creator/pull/29)
 - added: option to refer MessageRefs by name in LoadProcedure

v1.7.2
 - changed for Kaenx-Creator-Console
 - added: ParameterType Time

v1.7.1
 - feature: can import OGM-Common
 - feature: check errors have now link for dynamic item
 - changed: missing referenz will now load the file

v1.7.0
 - changed: only one application per file
 - added: output filename will be saved
 
v1.6.6
 - added: check for empty Parameter/ComObject Arg in Module
 - added: union memory view

v1.6.5
 - changed: knxprod.h will be saved in same folder as output.knxprod
 - fixed: Error on publish because HelpFile already exists
 - fixed: export for ETS6 (21/22) cant be imported
 - fixed: export HelpFiles

v1.6.4
 - added: Option for knxprod output path

v1.6.3
 - fixed: viewing a choose resets its parameterref

v1.6.2
 - fixed: cloning ComObjects
 - fixed: cloning Parameter
 - fixed: toggle AutoGenerate Refs
 - feature: Now real single file

v1.6.1
 - fixed: publishing with ETS6.1
 - fixed: you can now change NamespaceVersion even if the correct ETS is not installed

v1.6.0
 - added: french as Language
 - added: support for ETS 6.1
 - added: ability to sign folders
 - feature: defines in header to work with OpenKNX_Common
 - fixed: clone and copy/paste
 - fixed: export
 - fixed: scrollbar in some views
 - fixed: ComObject Number persistants
 - fixed: support for IP Routers
