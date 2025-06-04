import cv2
import numpy as np
import json

with open('regions.json', 'r') as file:
    regions = json.load(file)

map = cv2.imread("equirectangularprojection.png")
print(map.shape)

regions_prop = {
        region: {
        "contours": [(np.array(contour) / map.shape[1::-1]).tolist() for contour in regions[region]["contours"]],
            "hierarchy": regions[region]["hierarchy"]
        }
        for region in regions
}

with open('regions_prop.json', 'w') as file:
    json.dump(regions_prop, file)

map = cv2.imread("heightmap.png")

for region in regions_prop.values():
    contours = [(np.array(contour) * map.shape[1::-1]).astype(np.int32) for contour in region["contours"]]
    map = cv2.drawContours(map, contours, -1, (0, 255, 0), 3)

cv2.imshow("Map", map)
cv2.waitKey()

cv2.imwrite("heightmap_regions.png", map)
