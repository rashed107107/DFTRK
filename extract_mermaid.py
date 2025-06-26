#!/usr/bin/env python3
"""
Extract Mermaid content from HTML and provide conversion options
"""

import re
from pathlib import Path

def extract_mermaid_from_html():
    """Extract the mermaid diagram content from the HTML file"""
    html_file = Path("DFTRK_ERD.html")
    
    if not html_file.exists():
        print("❌ DFTRK_ERD.html not found!")
        return None
    
    content = html_file.read_text()
    
    # Find the mermaid content between script tags
    pattern = r'mermaid\.render\([^,]+,\s*`([^`]+)`'
    match = re.search(pattern, content, re.DOTALL)
    
    if match:
        return match.group(1).strip()
    
    # Alternative pattern if the first doesn't work
    pattern2 = r'```mermaid\n(.*?)\n```'
    match2 = re.search(pattern2, content, re.DOTALL)
    
    if match2:
        return match2.group(1).strip()
    
    print("❌ Could not extract mermaid content from HTML")
    return None

def create_mermaid_file(content):
    """Create a .mmd file with the mermaid content"""
    mermaid_file = Path("DFTRK_ERD.mmd")
    mermaid_file.write_text(content)
    print(f"✅ Created {mermaid_file}")
    return mermaid_file

def create_instructions():
    """Create instructions for converting to PNG/JPG"""
    instructions = """# How to Convert DFTRK_ERD.mmd to PNG/JPG

## Option 1: Online Mermaid Live Editor (RECOMMENDED - EASIEST)
1. Go to https://mermaid.live/
2. Delete the default content
3. Copy and paste the entire content from DFTRK_ERD.mmd
4. Click "Download PNG" or "Download SVG" 
5. If you need JPG, convert the PNG using any image converter

## Option 2: VS Code Extension
1. Install "Mermaid Preview" extension in VS Code
2. Open DFTRK_ERD.mmd in VS Code
3. Press Ctrl+Shift+P and search "Mermaid: Generate Preview"
4. Right-click the preview → "Export as PNG/SVG"

## Option 3: Browser Screenshot (Quick & Dirty)
1. Open DFTRK_ERD.html in your browser
2. Take a screenshot of the diagram
3. Crop and save as PNG/JPG

## Option 4: Install Node.js and mermaid-cli (Advanced)
1. Download and install Node.js from https://nodejs.org/
2. Open command prompt as Administrator
3. Run: npm install -g @mermaid-js/mermaid-cli
4. Run: mmdc -i DFTRK_ERD.mmd -o DFTRK_ERD.png

## Option 5: Online Image Converters
1. Use Option 1 to get SVG
2. Upload SVG to online converter like:
   - cloudconvert.com
   - convertio.co
   - Any SVG to PNG/JPG converter

## Recommended Approach:
Use Option 1 (mermaid.live) - it's free, requires no installation, and gives you high-quality images.
"""
    
    instructions_file = Path("ERD_Conversion_Instructions.txt")
    instructions_file.write_text(instructions)
    print(f"✅ Created {instructions_file}")

def main():
    print("🎯 DFTRK ERD to PNG/JPG Converter")
    print("=" * 50)
    
    # Extract mermaid content
    print("📄 Extracting Mermaid content from HTML...")
    mermaid_content = extract_mermaid_from_html()
    
    if not mermaid_content:
        return
    
    # Create .mmd file
    print("📝 Creating Mermaid file...")
    mermaid_file = create_mermaid_file(mermaid_content)
    
    # Create instructions
    print("📋 Creating conversion instructions...")
    create_instructions()
    
    print("\n✅ Setup complete!")
    print("\n📁 Files created:")
    print("   - DFTRK_ERD.mmd (Mermaid diagram file)")
    print("   - ERD_Conversion_Instructions.txt (How to convert to PNG/JPG)")
    
    print("\n🚀 QUICK START:")
    print("1. Go to https://mermaid.live/")
    print("2. Copy content from DFTRK_ERD.mmd")
    print("3. Paste it into the editor")
    print("4. Click 'Download PNG'")
    
    print(f"\n📊 Diagram size: {len(mermaid_content)} characters")

if __name__ == "__main__":
    main() 