package co.ab180.airbridge.unity;

import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.Iterator;
import java.util.List;
import java.util.Map;

import co.ab180.airbridge.event.Event;

public class AirbridgeEventParser {

    public static Event from(JSONObject object) throws IllegalArgumentException, JSONException {
        if (!object.has(AirbridgeConstants.Param.CATEGORY)) {
            throw new IllegalArgumentException("`Category` field cannot be null");
        }

        String category = object.getString(AirbridgeConstants.Param.CATEGORY);
        Event toReturn = new Event(category);

        if (object.has(AirbridgeConstants.Param.ACTION)) {
            String action = object.optString(AirbridgeConstants.Param.ACTION);
            if (!action.isEmpty()) {
                toReturn.setAction(action);
            }
        }

        if (object.has(AirbridgeConstants.Param.LABEL)) {
            String label = object.optString(AirbridgeConstants.Param.LABEL);
            if (!label.isEmpty()) {
                toReturn.setLabel(label);
            }
        }

        if (object.has(AirbridgeConstants.Param.VALUE)) {
            Object value = object.get(AirbridgeConstants.Param.VALUE);
            if (value instanceof Number) {
                toReturn.setValue((Number) value);
            }
        }

        if (object.has(AirbridgeConstants.Param.CUSTOM_ATTRIBUTES)) {
            Object value = object.get(AirbridgeConstants.Param.CUSTOM_ATTRIBUTES);
            if (value instanceof JSONObject) {
                toReturn.setCustomAttributes(toMap((JSONObject) value));
            }
        }

        if (object.has(AirbridgeConstants.Param.SEMANTIC_ATTRIBUTES)) {
            Object value = object.get(AirbridgeConstants.Param.SEMANTIC_ATTRIBUTES);
            if (value instanceof JSONObject) {
                toReturn.setSemanticAttributes(toMap((JSONObject) value));
            }
        }

        return toReturn;
    }

    private static Map<String, Object> toMap(JSONObject object) throws JSONException {
        Map<String, Object> toReturn = new HashMap<>();
        Iterator<String> keys = object.keys();
        while (keys.hasNext()) {
            String key = keys.next();
            Object value = object.get(key);
            if (value instanceof JSONArray) {
                value = toList((JSONArray) value);
            } else if (value instanceof JSONObject) {
                value = toMap((JSONObject) value);
            }
            toReturn.put(key, value);
        }
        return toReturn;
    }

    private static List<Object> toList(JSONArray array) throws JSONException {
        List<Object> toReturn = new ArrayList<>();
        for (int i = 0; i < array.length(); i++) {
            Object value = array.get(i);
            if (value instanceof JSONArray) {
                value = toList((JSONArray) value);
            } else if (value instanceof JSONObject) {
                value = toMap((JSONObject) value);
            }
            toReturn.add(value);
        }
        return toReturn;
    }
}



















