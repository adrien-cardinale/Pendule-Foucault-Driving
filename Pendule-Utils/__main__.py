import socket
import numpy as np
import matplotlib.pyplot as plt
import matplotlib.animation as animation
import re
import click
import time
import json
from . import Pendule
import threading

fig = plt.figure(figsize=(7, 7))
ax1 = fig.add_subplot(1, 1, 1)

pendule = Pendule()


def receiveDataThread():
    while (True):
        receive_data()
        time.sleep(0.05)


@click.group()
def cli():
    pass


@cli.command()
@click.option('-t', default=False, help='Trace')
def plot(t):
    ani = animation.FuncAnimation(
        plt.gcf(), update_plot, interval=50, fargs=(t,))
    plt.tight_layout()
    plt.show()


@cli.command()
@click.option('-i', default=100, help='Number of iterations')
def minmax(i):
    for j in range(i):
        receive_data()
    xmin, xmax, ymin, ymax = pendule.compute_min_max(i)
    print(f"Xmin: {xmin}, Xmax: {xmax}, Ymin: {ymin}, Ymax: {ymax}")


@cli.command()
@click.option('--iter', default=100, help='Number of iterations')
def mesure():
    data = []
    timer = time.time()
    for i in range(iter):
        position = receive_data()
        t = time.time() - timer
        data.append({"x": position[0][0], "y": position[1][0], "t": t})
    with open('data/data.json', 'w') as outfile:
        json.dump(data, outfile)


@cli.command()
def config():
    print("Make movement to axis x")
    input("Press Enter to continue...")
    pendule.configure("x")
    print("Make movement to axis y")
    input("Press Enter to continue...")
    pendule.configure("y")


def receive_data():
    message = clientsocket.recv(1024)
    message = message.decode('ascii')
    str = re.split(r'[,\n]', message)
    y = [float(str[0])]
    x = [float(str[1])]
    pendule.xArray.append(x)
    pendule.yArray.append(y)
    return x, y


def update_plot(i, trace=False):
    x = pendule.xArray[-1]
    y = pendule.yArray[-1]
    if not trace:
        ax1.clear()
    if i % 50 == 0:
        pendule.compute_min_max(100)
        global r
        r = np.sqrt((pendule.maxX - pendule.minX) ** 2 + (pendule.maxY - pendule.minY) ** 2) / 2
        ax1.clear()
    ax1.axis([-1700, 1700, -1700, 1700])
    circleMinMax = plt.Circle((pendule.circleYcenter, pendule.circleXcenter), r, color='b', fill=False)
    ax1.add_artist(circleMinMax)
    circleCenter = plt.Circle((pendule.circleXcenter, pendule.circleYcenter), 100, color='r', fill=False)
    if circleCenter.contains_point((y[0], x[0])):
        circleCenter.set_color('g')
        print("In the circle")
    ax1.add_artist(circleCenter)
    plt.gca().invert_yaxis()
    plt.plot(y, x, 'bo')


if __name__ == "__main__":
    print("Waiting for connection...")
    serversocket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    host = '192.168.125.1'
    port = 2090
    serversocket.bind((host, port))
    serversocket.listen(5)
    clientsocket, address = serversocket.accept()
    print(f"received connection from {address}")
    thread = threading.Thread(target=receiveDataThread)
    thread.daemon = True
    thread.start()
    cli()
