<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:wix="http://wixtoolset.org/schemas/v4/wxs">

  <!-- Key to find components containing TN_Doc.exe by their Id -->
  <xsl:key name="ExeComponents"
           match="wix:Component[wix:File[contains(@Source, 'TN_Doc.exe')]]"
           use="@Id"/>

  <!-- Identity transform: copy everything by default -->
  <xsl:template match="@*|node()">
    <xsl:copy>
      <xsl:apply-templates select="@*|node()"/>
    </xsl:copy>
  </xsl:template>

  <!-- Remove Component containing TN_Doc.exe (already defined in ServiceConfig.wxs) -->
  <xsl:template match="wix:Component[wix:File[contains(@Source, 'TN_Doc.exe')]]"/>

  <!-- Remove the corresponding ComponentRef that references the removed Component -->
  <xsl:template match="wix:ComponentRef[key('ExeComponents', @Id)]"/>

  <!--
    Note: cfg-elevator.exe is intentionally left in the harvest.
    It is installed as a regular file and deleted by MigrateCfg.ps1 after config migration.
    WiX gracefully handles missing files during uninstall.
  -->

</xsl:stylesheet>
