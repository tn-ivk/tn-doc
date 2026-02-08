<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:wix="http://wixtoolset.org/schemas/v4/wxs">

  <!-- Identity transform: copy everything by default -->
  <xsl:template match="@*|node()">
    <xsl:copy>
      <xsl:apply-templates select="@*|node()"/>
    </xsl:copy>
  </xsl:template>

  <!-- Remove Component containing TN_Doc.exe (already defined in ServiceConfig.wxs) -->
  <xsl:template match="wix:Component[wix:File[contains(@Source, 'TN_Doc.exe')]]"/>

</xsl:stylesheet>
