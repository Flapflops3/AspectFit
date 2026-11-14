using AspectWindow;

int assertions = 0;
void Check(bool condition, string label)
{
    assertions++;
    if (!condition) throw new Exception(label);
}

// 1280x720 client, 16px horizontal frame and 39px vertical frame.
Rect start = new(100, 200, 1396, 959);
foreach (int edge in Enumerable.Range(1, 8))
{
    Rect p = start;
    if (edge is 1 or 4 or 7) p.Left -= 160;
    if (edge is 2 or 5 or 8) p.Right += 160;
    if (edge == 3) p.Top -= 90;
    if (edge == 6) p.Bottom += 90;
    Rect r = ResizeGeometry.Constrain(p, start, edge, 16, 39, 16d/9, 320, 180);
    Check(r.Width == 1456 && r.Height == 849, $"edge {edge}: client must be 1440x810, got {r.Width-16}x{r.Height-39}");
    if (edge is 1 or 4 or 7) Check(r.Right == start.Right, $"edge {edge}: right anchor");
    if (edge is 2 or 5 or 8) Check(r.Left == start.Left, $"edge {edge}: left anchor");
    if (edge is 3 or 4 or 5) Check(r.Bottom == start.Bottom, $"edge {edge}: bottom anchor");
    if (edge is 6 or 7 or 8) Check(r.Top == start.Top, $"edge {edge}: top anchor");
    if (edge is 1 or 2) Check(r.Top + r.Bottom == start.Top + start.Bottom, "side resize centered vertically");
    if (edge is 3 or 6) Check(r.Left + r.Right == start.Left + start.Right, "top/bottom resize centered horizontally");
}
Rect verticalCorner = ResizeGeometry.Constrain(new(100,200,1396,1049),start,8,16,39,16d/9,320,180);
Check(verticalCorner.Width == 1456 && verticalCorner.Height == 849, "corner driven by vertical movement");
Rect tiny = ResizeGeometry.Constrain(new(100,200,120,240),start,8,16,39,16d/9,640,480);
Check(tiny.Width == 869 && tiny.Height == 519, "minimum height must also constrain width");
foreach (double ratio in new[]{4d/3,16d/10,16d/9,2580d/1080,3440d/1440})
foreach (int edge in Enumerable.Range(1,8))
foreach (int size in new[]{50,321,800,1365,2560,5000})
{
    Rect r=ResizeGeometry.Constrain(new(-200,-100,size-200,size-100),start,edge,24,58,ratio,320,200);
    Check(Math.Abs((r.Width-24)-(r.Height-58)*ratio) <= Math.Max(1,ratio), "client ratio with DPI frame and negative coordinates");
    Check(r.Width-24>=320 && r.Height-58>=200,"minimum bounds");
}
Rect unchanged=ResizeGeometry.Constrain(start,start,9,16,39,16d/9,1,1);
Check(unchanged.Width==start.Width && unchanged.Height==start.Height,"unrecognized sizing edge ignored");
Console.WriteLine($"PASS: {assertions} geometry assertions");
