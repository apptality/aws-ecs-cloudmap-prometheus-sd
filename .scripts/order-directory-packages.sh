#!/bin/bash
# Courtesy of https://gist.github.com/alex-tsbk/89a89cb319ef34fb38bc0aed54df9dbe
PKG_MANAGER=$(command -v dnf || command -v yum || command -v apt-get || command -v apt)

# Check if xmllint is installed, if not, install it
if ! command -v xmllint &> /dev/null; then
    echo "xmllint is not installed. Installing..."
    $PKG_MANAGER install -y libxml2-utils
fi

# Define the XML file path
xml_file="Directory.Packages.props"

# Use xmllint to parse XML, sort by Include, and deduplicate keeping highest version
xmllint --xpath "//PackageVersion" $xml_file |\
    sed 's/<PackageVersion/\n<PackageVersion/g' |\
    sort -t '"' -k 2,2 |\
    awk -F '"' '
        { if (last != $2) { if (last) print lastline; last=$2; lastver=$4; lastline=$0 }
          else { if ($4>lastver) { lastver=$4; lastline=$0 } } }
        END { print lastline }' |\
    sort -t '"' -k 2,2 > sorted_packages.xml

# Rebuild the XML structure with sorted and deduplicated packages
echo '<Project>' > new_$xml_file
echo '    <PropertyGroup>' >> new_$xml_file
echo '        <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>' >> new_$xml_file
echo '    </PropertyGroup>' >> new_$xml_file
echo '    <ItemGroup>' >> new_$xml_file
# For each line in sorted_packages.xml, append to new XML file
while IFS= read -r line; do
    echo "        $line" >> new_$xml_file
done < sorted_packages.xml
echo '    </ItemGroup>' >> new_$xml_file
echo '</Project>' >> new_$xml_file

# Optional: Replace old file with new file
mv new_$xml_file $xml_file

# Cleanup temporary file
rm sorted_packages.xml

echo "'${xml_file}' file has been re-ordered and deduplicated."
