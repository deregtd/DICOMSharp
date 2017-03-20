class Point3D implements IPoint3D {
    constructor(public xPos: number, public yPos: number, public zPos: number) {
    }

    clonePoint(): Point3D {
        return new Point3D(this.xPos, this.yPos, this.zPos);
    }

    addPoint(point: Point3D): Point3D {
        return new Point3D(this.xPos + point.xPos, this.yPos + point.yPos, this.zPos + point.zPos);
    }

    subtractPoint(point: Point3D): Point3D {
        return new Point3D(this.xPos - point.xPos, this.yPos - point.yPos, this.zPos - point.zPos);
    }

    multiplyBy(factor: number): Point3D {
        return new Point3D(this.xPos * factor, this.yPos * factor, this.zPos * factor);
    }

    divideBy(factor: number): Point3D {
        return new Point3D(this.xPos / factor, this.yPos / factor, this.zPos / factor);
    }

    cross(point: Point3D): Point3D {
        return new Point3D(
            this.yPos * point.zPos - point.yPos * this.zPos,
            this.zPos * point.xPos - point.zPos * this.xPos,
            this.xPos * point.yPos - point.xPos * this.yPos);
    }

    dot(point: Point3D): number {
        return this.xPos * point.xPos + this.yPos * point.yPos + this.zPos * point.zPos;
    }

    distanceFromOrigin(): number {
        return Math.sqrt(this.xPos * this.xPos + this.yPos * this.yPos + this.zPos * this.zPos);
    }

    normalized(): Point3D {
        const len = this.distanceFromOrigin();
        return new Point3D(this.xPos / len, this.yPos / len, this.zPos / len);
    }

    getPositionRelativeToPlane(planeOrigin: Point3D, planeR: Point3D, planeD: Point3D): Point3D {
        const p2 = this.subtractPoint(planeOrigin);
        const v3 = planeR.cross(planeD);

        //Solved system of three equations in mathematica to get this...
        //Essentially, the three scalar equations based off the vector formula: a*v1 + b*v2 + c*v3 = p2
        //Solve[{a*v1x + b*v2x + c*v3x == p2x, a*v1y + b*v2y + c*v3y == p2y, a*v1z + b*v2z + c*v3z == p2z}, {a, b, c}]

        const a = -1 * (p2.zPos * planeD.yPos * v3.xPos - p2.yPos * planeD.zPos * v3.xPos - p2.zPos * planeD.xPos * v3.yPos + p2.xPos * planeD.zPos * v3.yPos + p2.yPos * planeD.xPos * v3.zPos - p2.xPos * planeD.yPos * v3.zPos) /
            (planeR.yPos * planeD.zPos * v3.xPos + planeR.zPos * planeD.xPos * v3.yPos - planeR.xPos * planeD.zPos * v3.yPos - planeR.yPos * planeD.xPos * v3.zPos + planeR.xPos * planeD.yPos * v3.zPos - planeR.zPos * planeD.yPos * v3.xPos);

        const b = -1 * (p2.zPos * planeR.yPos * v3.xPos - p2.yPos * planeR.zPos * v3.xPos - p2.zPos * planeR.xPos * v3.yPos + p2.xPos * planeR.zPos * v3.yPos + p2.yPos * planeR.xPos * v3.zPos - p2.xPos * planeR.yPos * v3.zPos) /
            (planeR.zPos * planeD.yPos * v3.xPos - planeR.yPos * planeD.zPos * v3.xPos - planeR.zPos * planeD.xPos * v3.yPos + planeR.xPos * planeD.zPos * v3.yPos + planeR.yPos * planeD.xPos * v3.zPos - planeR.xPos * planeD.yPos * v3.zPos);

        const c = -1 * (p2.zPos * planeR.yPos * planeD.xPos - p2.yPos * planeR.zPos * planeD.xPos - p2.zPos * planeR.xPos * planeD.yPos + p2.xPos * planeR.zPos * planeD.yPos + p2.yPos * planeR.xPos * planeD.zPos - p2.xPos * planeR.yPos * planeD.zPos) /
            (planeR.yPos * planeD.zPos * v3.xPos + planeR.zPos * planeD.xPos * v3.yPos - planeR.xPos * planeD.zPos * v3.yPos - planeR.yPos * planeD.xPos * v3.zPos + planeR.xPos * planeD.yPos * v3.zPos - planeR.zPos * planeD.yPos * v3.xPos);

        return new Point3D(a, b, c);
    }
}

export = Point3D;
