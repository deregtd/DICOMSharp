function generateFunction(name, rgb, palette, planarone, grayscale, filter, mpr)
{
    var outstr = 'private _'+name+'() {\r\n';

    var additionalIndent = 0;
    function addLine(str) {
        for (var i=0; i<additionalIndent; i++)
        {
            str = '    ' + str;
        }
        outstr += str + '\r\n'
    }

    // Set up initial variables appropriate to the circumstance

    addLine('    const imWidth = this._renderImage.getWidth();');
    if (filter || planarone || mpr) {
        addLine('    const imHeight = this._renderImage.getHeight();');
    }

    if (filter) {
        // Offset when filtering since bilinear point centers aren't the same as the floor-based pixel offsets we're using,
        // so this way the centers of the bilinear filter look the same as the non-filtered version.
        addLine('    let trackingYImageX = this._imageAtTopLeft.xPos - 0.5;');
        addLine('    let trackingYImageY = this._imageAtTopLeft.yPos - 0.5;');
        if (mpr) {
            addLine('    let trackingYImageSlice = this._imageAtTopLeft.zPos - 0.5;');
        }
    } else {
        addLine('    let trackingYImageX = this._imageAtTopLeft.xPos;');
        addLine('    let trackingYImageY = this._imageAtTopLeft.yPos;');
        if (mpr) {
            addLine('    let trackingYImageSlice = this._imageAtTopLeft.zPos;');
        }
    }

    if (planarone) {
        addLine('    const frameOffset = imWidth * imHeight;');
    }

    addLine('    let pDataCoord = this._topY * this.props.panelWidth + this._leftX;');
    if (!mpr) {
        addLine('    const frameData = this._renderImage.frameData[this.state.imageFrame];');
    } else {
        addLine('    const imageSeries = this._imageSeries.images;');
    }

    // Run the main loop
    addLine('    for (let y = this._topY; y <= this._bottomY; y++) {');
    addLine('        let imageX = trackingYImageX;');
    addLine('        let imageY = trackingYImageY;');
    if (mpr) {
        addLine('        let imageSlice = trackingYImageSlice;');
    }
    addLine('        for (let x = this._leftX; x <= this._rightX; x++) {');
    if (mpr) {
        addLine('            let src = 0;');
        addLine('            if (imageX >= 0 && imageX < imWidth && imageY >= 0 && imageY < imHeight && imageSlice >= -0.5 && imageSlice < imageSeries.length) {');
        addLine('                let pSlice = imageSlice << 0;	// Math.floor');
        additionalIndent++;
    }
    addLine('            let pX = imageX << 0;	// Math.floor');
    addLine('            let pY = imageY << 0;	// Math.floor');

    if (filter) {
        addLine('            let pX1 = pX + 1;');
        addLine('            if (pX1 >= imWidth) {');
        addLine('                pX1 = imWidth - 1;');
        addLine('            }');

        addLine('            let pY1 = pY + 1;');
        addLine('            if (pY1 >= imHeight) {');
        addLine('                pY1 = imHeight - 1;');
        addLine('            }');

        if (mpr) {
            addLine('            let pSlice1 = pSlice + 1;');
            addLine('            if (pSlice1 >= imageSeries.length) {');
            addLine('                pSlice1 = imageSeries.length - 1;');
            addLine('            }');
        }

        addLine('            let xf: number, yf: number;');
        addLine('            if (imageX < 0) {');
        addLine('                pX = 0;');
        addLine('                xf = 0;');
        addLine('            } else {');
        addLine('                xf = imageX - pX;');
        addLine('            }');
        addLine('            if (imageY < 0) {');
        addLine('                pY = 0;');
        addLine('                yf = 0;');
        addLine('            } else {');
        addLine('                yf = imageY - pY;');
        addLine('            }');
        if (mpr) {
            addLine('            let slicef: number;');
            addLine('            if (imageSlice < 0) {');
            addLine('                pSlice = 0;');
            addLine('                slicef = 0;');
            addLine('            } else {');
            addLine('                slicef = imageSlice - pSlice;');
            addLine('            }');
        }

        addLine('            const w1 = (1.0 - xf) * (1.0 - yf);');
        addLine('            const w2 = (xf) * (1.0 - yf);');
        addLine('            const w3 = (1.0 - xf) * (yf);');
        addLine('            const w4 = (xf) * (yf);');
    }

    if (rgb || palette)
    {
        // TODO: unroll channel loop? prolly a decent speed bump from doing that, but verify.
        addLine('            let outVal = 0xff000000;');
        addLine('            for (let channel = 0; channel < 3; channel++) {');
        if (palette)
        {
            // TODO: Verify that you pull off the first entry instead of just ignoring it/grayscaling it
            addLine('                let psrc = frameData[pY * imWidth + pX] - this._paletteFirstEntry;');
            addLine('                if (psrc < 0) {');
            addLine('                    psrc = 0;');
            addLine('                } else if (psrc >= this._paletteNumEntries) {');
            addLine('                    psrc = this._paletteNumEntries - 1;');
            addLine('                }');
            addLine('                let src = this._palettes[channel][psrc];');

            if (filter)
            {
                addLine('                let psrc10 = frameData[pY * imWidth + pX1] - this._paletteFirstEntry;');
                addLine('                if (psrc10 < 0) {');
                addLine('                    psrc10 = 0;');
                addLine('                } else if (psrc10 >= this._paletteNumEntries) {');
                addLine('                    psrc10 = this._paletteNumEntries - 1;');
                addLine('                }');
                addLine('                let src10 = this._palettes[channel][psrc10];');

                addLine('                let psrc01 = frameData[pY1 * imWidth + pX] - this._paletteFirstEntry;');
                addLine('                if (psrc01 < 0) {');
                addLine('                    psrc01 = 0;');
                addLine('                } else if (psrc01 >= this._paletteNumEntries) {');
                addLine('                    psrc01 = this._paletteNumEntries - 1;');
                addLine('                }');
                addLine('                let src01 = this._palettes[channel][psrc01];');

                addLine('                let psrc11 = frameData[pY1 * imWidth + pX1] - this._paletteFirstEntry;');
                addLine('                if (psrc11 < 0) {');
                addLine('                    psrc11 = 0;');
                addLine('                } else if (psrc11 >= this._paletteNumEntries) {');
                addLine('                    psrc11 = this._paletteNumEntries - 1;');
                addLine('                }');
                addLine('                let src11 = this._palettes[channel][psrc11];');

                addLine('                src = ((src * w1) + (src10 * w2) + (src01 * w3) + (src11 * w4)) << 0;   // Math.floor');
            }
        } else if (rgb) {
            if (planarone)
            {
                // RRR GGG BBB
                addLine('                let src = frameData[frameOffset * channel + pY * imWidth + pX];');

                if (filter)
                {
                    addLine('                let src10 = frameData[frameOffset * channel + pY * imWidth + pX1];');
                    addLine('                let src01 = frameData[frameOffset * channel + pY1 * imWidth + pX];');
                    addLine('                let src11 = frameData[frameOffset * channel + pY1 * imWidth + pX1];');
                    addLine('                src = ((src * w1) + (src10 * w2) + (src01 * w3) + (src11 * w4)) << 0;   // Math.floor');
                }
            } else {
                // RGB RGB
                addLine('                let src = frameData[3 * (pY * imWidth + pX) + channel];');

                if (filter)
                {
                    addLine('                let src10 = frameData[3 * (pY * imWidth + pX1) + channel];');
                    addLine('                let src01 = frameData[3 * (pY1 * imWidth + pX) + channel];');
                    addLine('                let src11 = frameData[3 * (pY1 * imWidth + pX1) + channel];');
                    addLine('                src = ((src * w1) + (src10 * w2) + (src01 * w3) + (src11 * w4)) << 0;   // Math.floor');
                }
            }
        }

        addLine('                src = this._lut[src + this._lutZeroOffset];');
        addLine('                outVal |= src << (8 * channel);');
        addLine('            }');
    } else {
        // Grayscale
        if (mpr) {
            additionalIndent--;
            addLine('                src = imageSeries[pSlice].frameData[0][pY * imWidth + pX];');
            if (filter) {
                addLine('                let src10 = imageSeries[pSlice].frameData[0][pY * imWidth + pX1];');
                addLine('                let src01 = imageSeries[pSlice].frameData[0][pY1 * imWidth + pX];');
                addLine('                let src11 = imageSeries[pSlice].frameData[0][pY1 * imWidth + pX1];');
                addLine('                src = ((src * w1) + (src10 * w2) + (src01 * w3) + (src11 * w4)) << 0;	// Math.floor');

                addLine('                if (pSlice !== pSlice1) {');
                addLine('                    let src200 = imageSeries[pSlice1].frameData[0][pY * imWidth + pX];');
                addLine('                    let src210 = imageSeries[pSlice1].frameData[0][pY * imWidth + pX1];');
                addLine('                    let src201 = imageSeries[pSlice1].frameData[0][pY1 * imWidth + pX];');
                addLine('                    let src211 = imageSeries[pSlice1].frameData[0][pY1 * imWidth + pX1];');
                addLine('                    let src2 = ((src200 * w1) + (src210 * w2) + (src201 * w3) + (src211 * w4)) << 0;	// Math.floor');
                addLine('                    src = (src * (1.0 - slicef) + src2 * slicef) << 0;	// Math.floor');
                addLine('                }');
            }
            addLine('                src = this._lut[src + this._lutZeroOffset];');
            addLine('            }');
        } else {
            addLine('            let src = frameData[pY * imWidth + pX];');

            if (filter) {
                addLine('            let src10 = frameData[pY * imWidth + pX1];');
                addLine('            let src01 = frameData[pY1 * imWidth + pX];');
                addLine('            let src11 = frameData[pY1 * imWidth + pX1];');
                addLine('            src = ((src * w1) + (src10 * w2) + (src01 * w3) + (src11 * w4)) << 0;	// Math.floor');
            }
            addLine('            src = this._lut[src + this._lutZeroOffset];');
        }

        addLine('            let outVal = 0xff000000 | src | (src << 8) | (src << 16);');
    }

    addLine('            this._offScreenImageData32[pDataCoord++] = outVal;');
    addLine('            imageX += this._imagePitchRightward.xPos;');
    addLine('            imageY += this._imagePitchRightward.yPos;');
    if (mpr) {
        addLine('            imageSlice += this._imagePitchRightward.zPos;');
    }
    addLine('        }');

    addLine('        pDataCoord += this.props.panelWidth - 1 - this._rightX + this._leftX;');
    addLine('        trackingYImageX += this._imagePitchDownward.xPos;');
    addLine('        trackingYImageY += this._imagePitchDownward.yPos;');
    if (mpr) {
        addLine('        trackingYImageSlice += this._imagePitchDownward.zPos;');
    }
    addLine('    }');
    addLine('}');

    return outstr;
}

var totalFile = '// START GENERATED SECTION\r\n';
totalFile += '// Change anything in here by modifying the generator.js file and C/P the generated.txt file to here\r\n';

totalFile += generateFunction('renderToOffscreenCanvas_MPR_Grayscale_NoFilter', false, false, false, true, false, true);
totalFile += generateFunction('renderToOffscreenCanvas_MPR_Grayscale_Filter', false, false, false, true, true, true);
totalFile += generateFunction('renderToOffscreenCanvas_Grayscale_NoFilter', false, false, false, true, false, false);
totalFile += generateFunction('renderToOffscreenCanvas_Grayscale_Filter', false, false, false, true, true, false);
totalFile += generateFunction('renderToOffscreenCanvas_RGB_PlanarZero_NoFilter', true, false, false, false, false, false);
totalFile += generateFunction('renderToOffscreenCanvas_RGB_PlanarZero_Filter', true, false, false, false, true, false);
totalFile += generateFunction('renderToOffscreenCanvas_RGB_PlanarOne_NoFilter', true, false, true, false, false, false);
totalFile += generateFunction('renderToOffscreenCanvas_RGB_PlanarOne_Filter', true, false, true, false, true, false);
totalFile += generateFunction('renderToOffscreenCanvas_Palette_NoFilter', false, true, false, false, false, false);
totalFile += generateFunction('renderToOffscreenCanvas_Palette_Filter', false, true, false, false, true, false);
//totalFile += generateFunction(name, rgb, palette, planarone, grayscale, filter);

totalFile += '// END GENERATED SECTION\r\n';

var fs = require('fs');
fs.writeFile("generated.txt", totalFile, function (err) {
    if (err) {
        return console.log(err);
    }

    console.log("The file was saved!");
});
