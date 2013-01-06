<?xml version="1.0" encoding="iso-8859-1"?>
<xsl:stylesheet version="1.0"
  xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="xml" indent="yes" encoding="UTF-8" omit-xml-declaration="yes" />
  <xsl:template match="*">
    <resources>
      <string name="language">
        <xsl:value-of select="/root/Info/Name" />
      </string>
      <xsl:for-each select="/root/Texts/*">
        <string>
          <xsl:attribute name="name">
            <xsl:value-of select="name()" />
          </xsl:attribute>
          <xsl:value-of select="self::node()" />
        </string>
      </xsl:for-each>
    </resources>
  </xsl:template>
</xsl:stylesheet>