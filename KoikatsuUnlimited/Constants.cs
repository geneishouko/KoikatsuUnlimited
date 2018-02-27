using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KoikatsuUnlimited
{
    class Constants
    {
        public static readonly Dictionary<int, string> TextureTypes
        = new Dictionary<int, string>
        {
            { 400, "face_detail" },
            { 401, "eyeshadow" },
            { 402, "cheek" },
            { 403, "lip" },
            { 404, "lipline" },
            { 405, "face_paint" },
            { 406, "eyebrow" },
            { 407, "eye_white" },
            { 408, "eye" },
            { 409, "eye_gradation" },
            { 410, "eye_hi_up" },
            { 411, "eye_hi_down" },
            { 412, "eyeline_up" },
            { 413, "eyeline_down" },
            { 414, "nose" },
            { 415, "mole" },
            /*
            // Token: 0x040012D8 RID: 4824
            mt_body_detail = 420,
            // Token: 0x040012D9 RID: 4825
            mt_body_paint,
            // Token: 0x040012DA RID: 4826
            mt_sunburn,
            // Token: 0x040012DB RID: 4827
            mt_nip,
            // Token: 0x040012DC RID: 4828
            mt_underhair,
            // Token: 0x040012DD RID: 4829
            mt_pattern = 430,
            // Token: 0x040012DE RID: 4830
            mt_emblem,
            // Token: 0x040012DF RID: 4831
            mt_ramp,
            // Token: 0x040012E0 RID: 4832
            mt_hairgloss
*/
        };
    }
}
