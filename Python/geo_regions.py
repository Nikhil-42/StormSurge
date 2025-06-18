import geopandas as gpd
import numpy as np
import cv2
from shapely.geometry import LineString
from rasterio.features import rasterize
from rasterio.transform import from_bounds
from tqdm import tqdm

# --- Paths ---
shapefile_path = r"data/gadm_410-levels.gpkg"

# --- Step 1: Read input polygons ---
with tqdm(total=1, desc="Reading shapefile") as pbar:
    gdf = gpd.read_file(shapefile_path, layer="ADM_0")
    pbar.update(1)

if not gdf.crs:
    raise ValueError("Input shapefile has no CRS defined. Please ensure it has a valid CRS.")

# --- Step 2: Define rasterization grid ---
minx, miny, maxx, maxy = gdf.total_bounds
width, height = 10800, 5400   # adjust resolution as needed
transform = from_bounds(-180.0, -90.0, 180.0, 90.0, width, height)

# --- Step 3: Rasterize all polygons ---
raster = np.zeros((height, width, 3), dtype=np.uint8)
geoms = [(geom, 1) for geom in gdf.geometry]

for val, geom in tqdm(enumerate(gdf.geometry[:]), desc="Rasterizing polygons", total=len(gdf.geometry)):
    r, g, b = val = (val + 1).to_bytes(3, byteorder='big', signed=False)

    color = (r, g, b)
    mask = rasterize(
        [(geom, 1)],
        out_shape=(height, width),
        transform=transform,
        fill=0,
        all_touched=True,
        dtype=np.uint8
    )
    
    raster[mask > 0] = color

cv2.namedWindow("Rasterized Regions", cv2.WINDOW_GUI_NORMAL)
cv2.imshow("Rasterized Regions", raster)
cv2.waitKey(0)
cv2.imwrite("data/rasterized_regions.png", raster)
