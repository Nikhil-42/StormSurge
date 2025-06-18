from tkinter import W
import cv2
from itertools import product
import numpy as np
import json

borders_original = cv2.imread("data/rasterized_regions.png")
borders = borders_original.copy()

dragging = False
def on_click(event, x, y, flags, param):
    global dragging
    match event:
        case cv2.EVENT_LBUTTONDOWN:
            dragging = True
        case cv2.EVENT_LBUTTONUP:
            dragging = False

    if dragging:
        for dx, dy in product((-1, 0, 1), (-1, 0, 1)):
            point = (min(max(0, x + dx), borders.shape[1] - 1), min(max(0, y + dy), borders.shape[0] - 1))
            if (any(borders[*point[::-1]] != [0, 0, 0])):
                cv2.floodFill(borders, 255*np.ones(borders.shape[:-1]+(2,0)).astype(np.uint8), point, (255, 255, 255))


cv2.namedWindow("Borders", cv2.WINDOW_GUI_NORMAL)
cv2.setMouseCallback("Borders", on_click)

regions = {}
try:
    with open('regions.json', 'r') as file:
        regions = json.load(file)
except:
    pass

while True:
    cv2.imshow("Borders", borders)
    key = cv2.waitKey(20) 
    if key & 0xFF == ord('q'):
        break
    elif key & 0xFF == ord('y'):
        selection = borders[:, :] == (255, 255, 255)
        selection = np.all(selection, axis=2)
        print("Selection, shape: ", selection.shape, selection.dtype)
        mask = np.zeros(borders.shape[:-1], dtype=np.uint8)
        mask[selection] = 255

        mask = cv2.morphologyEx(mask, cv2.MORPH_CLOSE, np.ones((3, 3), np.uint8), iterations=5)

        contours, hierarchy = cv2.findContours(mask, cv2.RETR_CCOMP, cv2.CHAIN_APPROX_SIMPLE)
        polygons = np.zeros_like(borders)
        polygons = cv2.drawContours(polygons, contours, -1, (255,255,255), cv2.FILLED)

        cv2.namedWindow("Current Blob", cv2.WINDOW_GUI_NORMAL)
        cv2.imshow("Current Blob", polygons)
        if cv2.waitKey() & 0xFF == ord('y'):
            # Commit the region
            name = input("Name: ")
            borders[selection] = [0, 0, 0]
            regions[name] = {
                "contours": [(contour / [[borders.shape[1], borders.shape[0]]]).tolist() for contour in contours],
                "hierarchy": hierarchy.tolist()
            }
            with open('regions.json', 'w') as file:
                json.dump(regions, file)
        else:
            borders[selection] = borders_original[selection]
        cv2.destroyWindow("Current Blob")
