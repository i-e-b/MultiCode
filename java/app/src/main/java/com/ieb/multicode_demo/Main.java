package com.ieb.multicode_demo;

import android.app.ActionBar;
import android.app.Activity;
import android.os.Bundle;
import android.widget.TextView;

import com.ieb.multicode_demo.core.MultiCoder;

public class Main extends Activity {

    private String toHex(byte[] b){
        var sb = new StringBuilder();

        for (byte value : b) {
            int x = ((int) value) & 0xFF;
            sb.append(Integer.toHexString(x));
        }

        return sb.toString();
    }

    @Override
    protected void onCreate(Bundle bundle) {
        super.onCreate(bundle);

        var textView = new TextView(this);
        setContentView(textView);

        var original = new byte[]{(byte) 0xFE, (byte) 0xED, (byte) 0xFA, (byte) 0xCE};

        textView.append("\r\nSource data = "+ toHex(original));
        var encoded = MultiCoder.Encode(original, 6);
        textView.append("\r\nEncoded result as: "+encoded);

        var recovered1 = MultiCoder.Decode(encoded, 4, 6);

        textView.append("\r\nRecovered as: "+ toHex(recovered1));

        var broken = encoded.substring(1, 5) + "U" + encoded.substring(7);
        textView.append("\r\nDamaged result is: "+broken);
        var recovered2 = MultiCoder.Decode(broken, 4, 6);

        if (recovered2.length > 0){
            textView.append("\r\nRecovered as: "+ toHex(recovered2));
        } else {
            textView.append("\r\nFailed to recover data");
        }

        ActionBar bar = this.getActionBar();
        if (bar != null) this.getActionBar().setTitle("MultiCode demo");

    }
}
