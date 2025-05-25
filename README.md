# Paint Application 

## Project Description  
This Paint application is a simple graphics editor with a set of basic drawing and image editing tools. The project is implemented in C# using Windows Forms.  

## Key Features  

### Drawing Tools:  
- **Brush** – Draw freehand lines with customizable color and thickness  
- **Eraser** – Erase drawn elements  
- **Shapes**:  
  - Line  
  - Rectangle  
  - Circle  
- **Fill Tool** – Fill closed areas with the selected color  

### Selection & Editing Tools:  
- **Selection** – Select a rectangular area  
- **Cut** – Cut the selected area to the clipboard  
- **Copy** – Copy the selected area to the clipboard  
- **Paste** – Paste content from the clipboard  
- **Move** – Drag and drop the selected area on the canvas  

### Additional Features:  
- **Adjustable thickness** (1px, 3px, 5px, 8px)  
- **Color picker** with a standard dialog  
- **Undo/Redo** actions (limited to 10 steps)  
- **Canvas resizing**  
- **Save** images in JPG, PNG, or BMP formats  
- **Open** existing images  

Visual of paint
![image](https://github.com/user-attachments/assets/905462b8-a365-4f07-802c-2413ff19439c)


## Technical Details  
- Double buffering for smooth drawing  
- Optimized flood-fill algorithm using bitmap locking  
- Limited undo stack (10 most recent actions)  
- Unsaved changes detection on exit  
- Scroll support for large images  

## Requirements  
- .NET Framework  
- Windows OS  

## Installation & Running  
1. Clone the repository  
2. Open the solution in Visual Studio  
3. Build and run the project  

## How to Use  
1. Select a tool from the left panel  
2. For brush, eraser, and shapes – click and drag on the canvas  
3. For fill – click inside the area to fill  
4. For selection – click and drag to create a rectangular selection  
5. Use the **File** menu to create a new image, open, or save  

## License  
[MIT License](LICENSE)
