import cv2
import numpy as np
import json

with open('regions.json', 'r') as file:
    regions = json.load(file)

worldmap = cv2.imread("data/8081_earthmap10k.png")

for region in regions.values():
    contours = [(np.array(contour) * [[worldmap.shape[1], worldmap.shape[0]]]).astype(np.int32) for contour in region["contours"]]
    worldmap = cv2.drawContours(worldmap, contours, -1, (0, 255, 0), 3)

cv2.namedWindow("Map", cv2.WINDOW_GUI_NORMAL)
cv2.imshow("Map", worldmap)
cv2.waitKey()